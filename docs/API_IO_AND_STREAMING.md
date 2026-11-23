# API endpoint I/O and streaming contracts

This document records the stream and buffering contracts of the flows that exist in the repository today.
It adds no streaming framework. New incremental or realtime transports need a named consumer and measured
need first (see [Decisions not taken](#decisions-not-taken)).

## Media delivery (streamed)

Covers Catalog `GET /api/v1/public/catalog/media/{id}/{width}/{format}`, Catalog
`GET /api/v1/catalog/media/images/{id}/preview/{width}/{format}`, and the Public Web and Management Web
proxy endpoints that forward them.

| Concern | Contract | Tested in |
| --- | --- | --- |
| Direction and consumer | Server to browser, one image rendition per request | all rows below |
| Result type | `Results.Stream(content, contentType, enableRangeProcessing: false)` | `MediaStreamingContractTests`, proxy endpoint tests |
| Range | Not supported. A `Range` request returns `200` with the full body, no `Accept-Ranges`, no `Content-Range` | `MediaStreamingContractTests`, `PublicWebEndpointTests`, `ManagementWebEndpointTests` |
| Ownership and disposal | The opened object stream (Catalog) or upstream `PublicMediaObjectResponse` (proxies) is registered with `Response.RegisterForDispose[Async]` and disposed when the response completes | same |
| Upstream buffering | Proxies and typed clients use `ResponseHeadersRead` and `ReadAsStreamAsync`; the body is never copied into memory | disposal only: `PublicMediaObjectResponseTests`; buffering by code inspection |
| Headers | `X-Content-Type-Options: nosniff`; Catalog and Management responses are `no-store`; Public Web applies its published-content cache policy | `CatalogApiEndpointTests`, `PublicWebEndpointTests`, `ManagementWebEndpointTests` |
| Content type | Stored type must equal the requested rendition type, otherwise `404` | `CatalogApiEndpointTests` |
| Cancellation and disconnect | When the client disconnects mid-body, the stalled object read is cancelled through `RequestAborted` and the registered disposal still runs; the response is never buffered, so a slow consumer holds one pending read, not queued data | `MediaStreamingContractTests` (Catalog); proxies by code inspection |
| OpenAPI | `200` declares `image/avif`, `image/jpeg`, `image/png`, and `image/webp` as binary (`Stream` schema) and `404`; the preview also declares `400`. The generated artifacts are committed under `src/ViajantesTurismo.Catalog.Contracts.Http/OpenApi/`. No client is generated from them | `CatalogOpenApiMediaDeliveryContractTests` |
| Resume | None. Clients refetch the whole rendition | `MediaStreamingContractTests` |

## Buffered flows

| Flow | Direction | Bound | Why buffered | Decision |
| --- | --- | --- | --- | --- |
| Customer CSV import (`CustomerImportEndpoints.ReadCsv`) | Browser to API | `ContractConstants.CustomerImportMaxFileBytes` = 1 MiB, enforced while reading; oversize and cancellation tested. Three passes: bounded `SeekableContent` read, malware scan, `ReadToEnd` into a string | Malware scanning needs a seekable stream and the parser needs the whole text | Keep buffering. A 1 MiB file bounds the request at about 1 MiB of bytes plus about 2 MiB of string. |
| Finalized document download (`DocumentEndpoints`, `DocumentsApiClient`, Management Web proxy) | API to browser | The API checks one sealed body against its recorded length and SHA-256, then serves the verified `ReadOnlyMemory<byte>`. The Management client buffers the response. The template measured 49,481 bytes with the three staff-editable fields at 4,000 `<` characters each. Non-editable fields such as tour name and included services were below their maximum. | A newly finalized artifact is capped at 1 MiB and audited as one value. | Revision and list reads load artifact availability without the bytes. |
| Branding logo upload (`BrandingLogoIntake.Process`, `PUT /api/v1/branding/settings/logo`) | Browser to API | `BrandingLogoLimits.MaxUploadBytes` = 1 MiB, enforced by `RequestSizeLimitAttribute` and by the `SeekableContent` byte limit; 256 KiB memory spool threshold. Raw request body, not multipart | The signature check, malware scan, animation chunk walk, and decode need several seekable passes | Keep the lease. See [Branding logo flow](#branding-logo-flow). |

Any change to these flows must keep malware scanning, the size limit, authorization, audit, privacy, and
`no-store` headers.

The artifact migration copies sealed bytes to `DocumentArtifactBodies` in the same transaction that removes the old
column. The size constraint applies to subsequent writes. Existing bytes, including artifacts above the new 1 MiB cap,
remain available under the same authorization and lifecycle rules. The reverse migration copies the bytes back.
[PostgreSQL check constraints on existing data](https://www.postgresql.org/docs/current/sql-altertable.html#SQL-ALTERTABLE-NOTES)

### Customer import circuit retention

The Management Web component holds the selected CSV as `ReadOnlyMemory<byte>` while the user
reviews duplicates. It records the remapped header names and only the row fields changed by
completed mixed decisions. Each upload reconstructs the effective CSV from that state. Canonical
uploads share the selected buffer through the multipart client. When the mapping or mixed
choices change the CSV, encoding produces one temporary upload payload.

The column review reuses the selection-time CSV parse to show known header labels and suggestions
based on the shape of at most the first five data rows. Management Web evaluates those samples
locally and shows the reason without displaying individual values until the normal preview.
Staff resolve ambiguous mappings and confirm the preview before the Admin
endpoint receives the file. The Admin endpoint still scans the upload before it parses or stores data.

The component test holds independent imports at their HTTP boundary until all are in flight.
It then releases the responses, waits for duplicate resolution, and counts the distinct
backing arrays retained by each component's workflow state. With 1 MiB files whose headers
require remapping, the retained payload dropped from 2 MiB to 1 MiB per circuit.

| Concurrent imports | Before | After |
| ---: | ---: | ---: |
| 1 | 2 MiB | 1 MiB |
| 4 | 8 MiB | 4 MiB |
| 8 | 16 MiB | 8 MiB |

The canonical control retained one 1 MiB buffer in both runs. These figures cover retained CSV
arrays at duplicate resolution. Transient parser, encoder and transport allocations have their
own lifetimes.

```bash
dotnet test --project tests/ViajantesTurismo.Management.WebTests/ViajantesTurismo.Management.WebTests.csproj \
  -c Release --filter-class '*ImportCustomersBufferRetentionTests' --report-xunit-xml
```

## Upload and import pass trace

Measured for the upload and import performance epic with a counting, non-seekable source stream and recording scanner and object-store
doubles. The traces were throwaway tests and are not committed. Figures are managed allocations on the calling
thread (`GC.GetAllocatedBytesForCurrentThread`) from one Debug-build run per size, so treat them as ratios, not
budgets.

| Flow | Request source reads | Buffer | Passes over the buffer | Managed allocation |
| --- | --- | --- | --- | --- |
| Customer CSV import (`ReadCsv`) | 1 pass, 80 KiB chunks (14 reads at 1 MiB) | One `MemoryStream`, capacity 1,310,720 at 1 MiB (growth slack) before presizing | Scan, then `ReadToEnd` into a string | 510,104 B at 64 KiB; 6,927,192 B at 1 MiB (about 6.6 times the payload, transient) |
| Media upload intake (`MediaImageUploadIntake.Accept`) | 1 pass (65 reads at 5 MiB) | One `MemoryStream`, capacity equal to length at 5 MiB, 1,310,720 at 1 MiB before presizing | 12-byte header peek, scan, decode probe, SHA-256, object store `Put`; scanner and store receive the same instance | 247,168 B at 64 KiB; 2,721,224 B at 1 MiB; 10,603,960 B at 5 MiB (about 2 times the payload) |
| Branding logo upload (`BrandingLogoIntake.Process`) | 1 pass over the raw `HttpRequest.Body` through `SeekableContent` (spools past 256 KiB) | The lease content: memory up to 256 KiB, then a private temporary file | 12-byte signature peek, scan, animation chunk walk (rewinds to 0), decode and re-encode, SHA-256 of the output | Not measured. The re-encoded result is at most 40 KiB |

Findings:

- Presizing the buffer to the received length and borrowing the copy buffer from `ArrayPool` removed the growth
  slack. On a warm run the media upload at 5 MiB fell from about 10.5 MB to 5.3 MB and the CSV import at 1 MiB from
  about 9.0 MB to 5.2 MB. The figures in the table above are the earlier measurement.
- The dominant remaining CSV cost is parse count, not buffering. The text is parsed at least three times per import
  request (conflict detection, workflow, command handler), about 25 MB for a 1 MiB file. Parsing from the UTF-8 bytes
  once costs about 4.5 MB (`CsvParseBenchmarks` in the file-scanning benchmark project).
- Both flows read the request stream exactly once and never copy the buffer a second time. The media scanner and the
  object store receive the same `MemoryStream`, positioned at zero.
- Retained memory per request is bounded by the configured maximum: 1 MiB (CSV, plus about 2 MiB of string) and
  `ContractConstants.MaxMediaUploadBytes` = 5 MiB (media). The extra transient allocation was `MemoryStream` growth.
- The local object store rewinds a seekable stream, copies at most the declared length into a temporary file while hashing it,
  refuses a shorter or longer stream and a wrong `sha256:` checksum (deleting the temporary file), and then moves it into place.
- Not measured here: native Magick.NET memory and the SharedKernel S3 provider. Concurrent-request memory and
  pre-handler time are covered in the next section.

### Concurrency measurement on a production-shaped scratch host

A throwaway Kestrel host approximated the media intake: `IFormFile` binding, a presized `MemoryStream` filled by an
80 KiB `ArrayPool` copy loop, one scan pass and one SHA-256 pass. It is not the real application: there is no
Magick.NET decode, no object store, and no real scanner. k6 v2.2.0 and the host ran on the same WSL2 machine (16
logical cores, 24 GB, .NET SDK 10.0.402) over loopback. Each cell is one run of a 15 second window after a 3 second
warm-up, against a fresh host per level, so the figures are indicative only. Peak RSS delta is the peak resident set
size above the idle host.

| Upload size | In flight (VUs) | p95 latency | Peak RSS delta | Notes |
| --- | --- | --- | --- | --- |
| 5 MiB | 1 | 46 ms | 10 MiB | 0 errors |
| 5 MiB | 4 | 151 ms | 38 MiB | 0 errors |
| 5 MiB | 16 | 269 ms | 192 MiB | GC pause about 12% of wall time |
| 5 MiB | 64 | 931 ms | 535 MiB | GC pause about 19% of wall time |
| 1 MiB | 1 / 4 / 16 / 64 | 11 / 24 / 67 / 182 ms | 8 / 77 / 121 / 216 MiB | GC pause about 23% of wall time at 64 |

Findings:

- Memory scales roughly linearly with uploads in flight: about 8.4 to 12 MiB per in-flight 5 MiB upload, which is 1.7
  to 2.4 times the documented one-buffer bound. Nearly all collections were gen2 because the buffers live on the large
  object heap. No run produced an error or an out-of-memory condition.
- Time before the handler runs (body receive plus form buffering) was 83 to 88 percent of request time. The
  unbuffered stock benchmark host has similar latency (p50 32 ms against 38 ms at 5 MiB, 1 VU), so this is mostly
  transfer time and not an isolated buffering cost.
- `IFormFile.OpenReadStream()` returned a seekable `ReferenceReadStream`, so the framework has already buffered the
  file and the intake copy into a `MemoryStream` is a second copy.
- Neither measurement host has a rate limiter or authentication, so the runs were unthrottled and represent an upper
  bound on raw capacity, not production behavior.
- The production endpoints are bounded only by per-IP fixed-window rate limits: the media upload shares the Catalog
  mutation policy of 20 requests per minute (`CatalogSecurityBaseline.cs`) and the CSV import uses 5 requests per
  minute (`AdminSecurityBaseline.cs`). These cap the request rate per client IP, not concurrent in-flight requests.
  Both endpoints also require authorization, which limits who can upload but does not bound concurrency. Request sizes
  are capped by `RequestSizeLimitAttribute` (5 MiB plus 16,384 bytes for media, `CustomerImportMaxRequestBytes` for
  import).
- No concurrency limiter, `MaxConcurrentConnections`, `FormOptions` or `MultipartBodyLengthLimit` override, or
  container memory limit was found in the repository. Realistic production concurrency is an estimate, not a
  measurement.
- Limits of the measurement: one run per cell, an approximation host, loopback with the load generator on the same
  machine, and no `dotnet-counters` collection.
- The stock benchmark host measures only the Kestrel streaming floor because it does not mirror the production intake
  shape.

Direct-read experiment (same scratch host, 5 MiB, three repeats per cell, medians, 0 errors): reading from the
buffered `IFormFile` stream (counting pass, rewind, scan, rewind, hash) instead of copying into a `MemoryStream` cut
allocation from 5.95 GB to 0.34 GB at 16 uploads in flight and from 7.91 GB to 0.49 GB at 64. Gen2 collections fell
from 96 to 4 and from 94 to 8, GC pause from 2.8 s to 0.13 s and from 3.8 s to 0.32 s, and peak memory above idle
from 232 to 54 MB and from 452 to 125 MB. At 1 VU the direct route was slower in the median (13.0 against 18.9
requests per second) with overlapping ranges; at 4, 16 and 64 VUs latency and throughput were better or within noise.
Exact-limit input was accepted, limit plus one byte was rejected without scanning, and an empty file reached the scan
on both routes (production validation rejects length 0 earlier). The counters include startup and warm-up. The host
has no Magick.NET decode, real scanner, object store or validator. The CSV import cannot take this path as is,
because its result outlives the stream and is built from `GetBuffer()`.

Decision: adopt `PandaBear.IO.SeekableContent` for both server intakes. The first version of the primitive always copied, which
would have added a third copy of a stream the framework already buffers, so it was not adopted. The pinned version passes a
seekable source at position zero through after one bounded counting pass and spools anything else: a non-seekable source, or a
seekable one at a non-zero position, goes to memory up to a threshold and then to a private temporary file. The lease never
disposes the source, so the endpoint keeps owning and disposing `IFormFile.OpenReadStream()`.

- Media upload (`MediaImageUploadIntake.Accept`) leases the content with a 1 MiB spool threshold
  (`SpoolMemoryThresholdBytes`), against the 5 MiB maximum. A seekable upload is never copied. `Scan` and `ReadHeaderBytes`
  take `Stream`. Oversize input maps to the same invalid result as before, the validator runs before the scan, and every pass
  rewinds explicitly. The scanner, decode probe, hash and object store all receive the caller's stream instance.
- Customer CSV import (`CustomerImportEndpoints.ReadCsv`) uses the same lease with a threshold equal to the 1 MiB file maximum.
  The scanner sees the raw bytes first; only after it approves is one array of the content allocated, so the result outlives
  the stream and scan-before-parse is preserved. The CSV flow gains consolidation (one tested bounded read instead of a private
  copy loop) but no allocation saving: it still allocates one content-sized array. The proven gap in the CSV flow remains
  repeated parsing, not buffering.
- Both private `CopyToMemory` loops are gone. Declared-versus-received length, the size limit, cancellation, fail-closed
  scanner handling and the non-seekable path keep their tests.

Measurement of the adopted pattern: the benchmark host gained a third mode, `VT_UPLOAD_MODE=seekable`
(`POST /upload/scan-seekable`), which uses the real `SeekableContent` from the pinned package. It runs next to `buffered`, which
still mirrors the old copy into a presized `MemoryStream`, and a `GET /diagnostics` route reports the process allocation, GC
collection counts and total GC pause. Each cell below is the median of three runs of a 15 second window after a 3 second
warm-up, 5 MiB payload, against a fresh host per run, with `buffered` and `seekable` alternating on the same 16-logical-core
shared WSL2 machine, k6 v2.2.0 on loopback; 0 failed requests in all 18 runs. Counters are deltas over the 15 second window;
peak RSS is the process high-water mark and includes startup and warm-up.

| In flight (VUs) | Mode | Allocated | Gen2 collections | GC pause | Peak RSS | Requests per second | p95 latency |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | buffered (old) | 1.81 GB | 170 | 0.10 s | 115 MB | 21.7 | 51 ms |
| 1 | seekable (new) | 0.13 GB | 0 | 0.01 s | 104 MB | 18.7 | 61 ms |
| 16 | buffered (old) | 7.70 GB | 84 | 2.00 s | 315 MB | 93.2 | 294 ms |
| 16 | seekable (new) | 0.31 GB | 1 | 0.09 s | 153 MB | 95.8 | 312 ms |
| 64 | buffered (old) | 10.21 GB | 53 | 2.34 s | 900 MB | 122.8 | 1,090 ms |
| 64 | seekable (new) | 0.67 GB | 1 | 0.20 s | 220 MB | 135.8 | 811 ms |

At 1 VU the new pattern was slower in the median (18.7 against 21.7 requests per second, p95 61 against 51 ms) because of the
extra counting pass; at 16 and 64 VUs throughput and latency were within noise or better. The absolute numbers differ from the
earlier direct-read experiment because the runs are different and shorter, but the direction and size of the effect agree.
These are indicative single-machine scratch results on a shared host, not production budgets. The host has no Magick.NET decode,
real scanner, object store, validator, rate limiter or authentication, so the real endpoint will show a smaller relative gain.
Not measured: native Magick.NET memory, what `new MagickImage(stream)` buffers internally, and the SharedKernel S3 provider.

The regression guard for the media gap is a unit test that runs the intake over a 4 MiB seekable stream and asserts the managed
allocation on the calling thread stays far below the payload. Before the change that run allocated 4,211,544 bytes. Preserve the
size checks, the scan-before-parse order for CSV, actual-length comparison, cancellation, and the non-seekable path in any later
change.

## Catalog media intake flow

`MediaImageUploadIntake.Accept` handles the Catalog gallery upload:

1. Bounded read through `SeekableContent` (limit `MediaUploadValidationOptions.MaxLengthBytes`), then `MediaUploadValidator`
   checks the declared type against the allow-list (AVIF, JPEG, PNG, WebP), the file extension against that type, and the
   first 12 bytes against the type's signature. A signature that belongs to a different allowed type is refused with the fixed
   copy "File signature does not match the allowed content type." and nothing from the upload is echoed.
2. Pass the configured malware scan and check for a matching upload in the authorized gallery. A matching upload reuses the
   existing image if it meets the current decode limits. See
   [Duplicate Catalog uploads](media-image-processing.md#duplicate-catalog-uploads).
3. Sanitize and decode a new image with pixel limits. The sanitizer identifies the actual image format and ImageMagick
   decodes it for the size check. The stored `ContentType` is the canonical lower-case type, such as `image/jpeg`.
4. Hash, then `IObjectStore.Put`, then outbox and metadata in `Compensation.CompleteOrCompensate`. If a later step fails or the
   request is cancelled, the object is deleted on a separate token that times out after 10 seconds
   (`MediaObjectCompensation`), so a cancelled request still removes the object it wrote. The original exception keeps flowing;
   a delete that fails is left to `MediaObjectReconciliationService`. The variant handler compensates its variant objects the
   same way.
5. The existing upload transaction commits the new image, its legacy links, its processing wake-up, and the ordered photo
   append with its separate placement wake-up. The Catalog tour mutation lease serializes acceptance with other tour edits.
   Duplicate uploads exit before writing either wake-up. The placement write uses identifiers only. The bounded read, scan,
   sanitize, decode, hash and object storage passes are unchanged.

Animated PNG and WebP uploads are accepted. Catalog serves a still rendition built from the first
frame. The shared upload picker tells staff this for file selection, folder selection, and drops.
See [Media image processing flow](media-image-processing.md#animated-images). AVIF image sequences are not detected.

## Branding logo flow

`PUT /api/v1/branding/settings/logo` (`BrandingEndpoints.UploadLogo`) takes the image as the raw request body, not as
multipart. `BrandingLogoIntake.Process` runs these steps; each rewinds the lease before the next:

1. Declared `Content-Type` is parsed as a media type and must be `image/png`, `image/jpeg`, or `image/webp` (case-insensitive;
   parameters such as `charset` are ignored). A missing, malformed, comma-separated or other value, including SVG, is `400` on
   `ContentType`.
2. `SeekableContent.Create` copies the body with a 1 MiB limit and a 256 KiB memory spool threshold. An empty or oversize body
   is `400` on `Content`. The endpoint also carries `RequestSizeLimitAttribute` at 1 MiB.
3. Signature check of the first 12 bytes against the declared type.
4. Malware scan of the lease: rejected is `400`, a scanner failure is `503`, and the failure type is the only thing logged.
   Nothing past the signature peek is parsed before the scan approves.
5. Animation chunk walk for PNG (`acTL`) and WebP (`VP8X` flag, `ANIM`, `ANMF`); animated, truncated or overrunning files are
   `400`. The walk is bounded by the 1 MiB limit and built on the pure functions in `ImageChunkParser`. See
   [Upload and binary data guidelines](UPLOAD_AND_BINARY_DATA_GUIDELINES.md#intake-order).
6. Decode within 2048 x 2048 pixels and 4,000,000 pixels, then re-encode to WebP trying seven size and quality variants until
   one is at most 40 KiB; otherwise `400`.
7. The result is a `BrandingLogo` (`ReadOnlyMemory<byte>`) addressed by its SHA-256 and stored once in
   `branding."BrandingLogos"` (`bytea`, written from an internal `byte[]`).
8. `GET /api/v1/public/branding/logo/{sha256}` serves it with a long `immutable` client cache, a strong `ETag`, and `nosniff`.
   On an output-cache miss, an anchored pattern validates the address before the store is read. The store checks the WebP
   media type and hashes the stored bytes before serving the logo.

The endpoint requires `BrandingWrite`, `If-Match`, and the Branding mutation rate limit. The flow is documented in
[Branding](branding.md#logo).

## Upload disconnect, slow clients, and timeouts

`CustomerImportUploadOverKestrelTests` (Admin) and `MediaUploadOverKestrelTests` (Catalog) drive the customer import
(`/customers/import`, `/customers/import/commit`) and tour image upload endpoints over real HTTP on a loopback Kestrel
server, not TestServer, with fake scanners and stores.

| Behavior | Observed contract |
| --- | --- |
| Client aborts mid-body | The request ends before the endpoint body runs: either the request token fires or the framework answers the incomplete multipart body with `400`, and which one is visible first is a race. The scanner is never called and nothing is persisted. The framework reads the form before the endpoint body, so the endpoint never starts |
| Slow client, small chunks | Completes; the scanner receives the exact bytes once, only after the last chunk was sent. Body reads are at most 81,920 bytes |
| Request over the `RequestSizeLimit` | `413` from Kestrel before the body is read; no scan, nothing persisted |
| File over the file limit but within the request limit | `400`; no scan, nothing persisted |
| Multipart form above its section count or conflict-resolution budget | `400`; no scan, nothing persisted |
| Stalled client below a minimum body data rate | `408` from Kestrel; no scan, nothing persisted |
| Multipart upload reaches its application deadline while reading the body | `504`; no scan, nothing persisted |

`BrandingLogoUploadOverKestrelTests` drives the raw logo `PUT` through the same loopback wire harness.
Kestrel returns `413` for oversized uploads with a declared content length and for chunked uploads.
The declared-length request is rejected before any body read. A client below the configured minimum body data rate
receives `408`. Oversized, stalled, and mid-body disconnect requests leave the scanner and store untouched.

The slow-client test gates each chunk and checks that the scanner reads the original image once, after the final chunk.
Cancellation while scanning closes the file-backed lease, deletes its temporary file, and leaves the logo store empty.

### Transport and form limits

Admin and Catalog use an 8 MiB Kestrel request body backstop. This leaves about 3 MiB of headroom
above the largest current endpoint budget and covers endpoints with no explicit content budget.
The framework defaults are 30,000,000 bytes and 240 bytes per second with a five-second grace
period. Both hosts require 1 KiB per second and use the five-second grace period for the initial
burst. They pin the framework's 30-second header deadline and 130-second idle keep-alive timeout.
The Admin and Catalog multipart wire tests exercise the configured rate on their real hosts with
no test override.
See [Kestrel server limits](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits?view=aspnetcore-11.0).

| Endpoint | Total request body | Multipart section size | Count of multipart sections |
| --- | ---: | ---: | ---: |
| Customer import | 1 MiB + 32 KiB | 1 MiB | 1 file |
| Customer import commit | 1 MiB + 32 KiB | 1 MiB | 1 file and 1 optional conflict-resolution field |
| Catalog tour image upload | 5 MiB + 16 KiB | 5 MiB | 1 file and up to 4 text fields |
| Branding logo upload | 1 MiB | Raw image body | Raw image body |

ASP.NET Core applies the multipart section limit to file and text parts. Its `ValueLengthLimit`
and `KeyLengthLimit` apply to URL-encoded forms. For multipart forms, the section and total body
limits cap text bytes during parsing, and the framework's per-section header limit bounds field
names. Import commit checks the decoded conflict resolutions' UTF-8 byte count against the 16 KiB
field budget before scanning. Catalog retains its field-specific domain validation for captions,
alternative text, attribution, and copyright. The multipart memory threshold stays at 64 KiB and
larger file sections spool to temporary storage.
See [ASP.NET Core form parsing](https://github.com/dotnet/aspnetcore/blob/v11.0.0/src/Http/Http/src/Features/FormFeature.cs).

### Application deadlines

Customer import, import commit, and Catalog tour image uploads have a two-minute application
deadline. This gives the 5 MiB maximum multipart upload time to transfer alongside ClamAV's default
30-second scan, validation, processing, and storage. The request body passes the deadline token into
the framework's multipart reads without adding a buffer. The multipart deadline wire tests shorten
the policy and leave the client connected with a partial form to verify the `504` response.

The Branding logo `PUT` has a 60-second application deadline. This gives the 1 MiB upload,
image processing, and storage a budget alongside ClamAV's default 30-second scan timeout.
Two Kestrel tests shorten the named policy to verify cancellation during body intake and scanning.
The scan test also verifies removal of the temporary file. All application upload deadlines start after
authorization and rate limiting. If cancellation ends the request before a response starts, the request
timeout middleware returns `504`.
See [ASP.NET Core request timeouts](https://learn.microsoft.com/aspnet/core/performance/timeouts).

### Rate-limit partitioning and browser requests

Admin imports share five requests per minute per remote IP. Catalog image uploads share Catalog's
mutation budget of 20 requests per minute per remote IP. Branding management settings reads and
updates, logo uploads, and logo deletion share a separate budget of 20 requests per minute per remote IP.
Each API applies trusted forwarded headers before evaluating the partition. Clients behind the same
proxy or NAT share a budget if the API observes the same remote address. These fixed windows are
request admission budgets. The separate in-flight limit decision needs a deployed replica memory budget
and measured memory use with the production host, scanner, and stores.

Branding logo `PUT` and `DELETE` use the Branding CORS policy and require a bearer token with
Branding write permission. Browsers may preflight requests from origins listed in
`Security:Cors:AllowedOrigins`. CORS responses omit `Access-Control-Allow-Credentials`. The Management Web BFF
applies antiforgery to its own cookie-authenticated surface.

## Decisions not taken

No `IAsyncEnumerable<T>` endpoint, SSE endpoint, WebSocket protocol, SignalR hub, gRPC streaming, or
HTTP/3 mandate exists or is planned. Each needs a named consumer, direction, media type, limits,
cancellation, disconnect, resume, and error semantics, and HTTP/2 or HTTP/3 needs evidence through the
complete proxy and load-balancer path with fallback retained.

### Media delivery protocol

Keep the current protocol configuration for Public Web media and the Management Web document proxy.
The first delivery case to reconsider is a visitor opening a tour with several Catalog image renditions
through Public Web's `/catalog/media/{id}/{width}/{format}` endpoint.

A local .NET 11.0.100 probe ran the real Public Web route on loopback Kestrel with a synthetic Catalog
stream. One reused client requested exact HTTP/1.1. After five warm-up requests per size, 30 sequential
requests per size returned `200`, negotiated HTTP/1.1, and delivered the complete body. The measurements
span sending the request through reading the final byte.

| Rendition body | Median | 95th percentile |
| --- | ---: | ---: |
| 64 KiB | 0.243 ms | 0.389 ms |
| 512 KiB | 0.562 ms | 1.291 ms |

This baseline covers the route and its downstream client connection on one machine. A production
comparison needs the selected edge and load balancer, an actual Catalog store, and parallel image
requests under representative bandwidth and latency. Run each configuration through the complete route,
record negotiated protocols at each hop, and confirm the existing fallback on clients without newer
protocol support. Repeat authorization, cache, range, and disconnect checks before changing either delivery
path. The [production readiness record](operations/production-readiness.md) tracks the deployment target.
[Kestrel HTTP/3 deployment guidance](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/http3)

## Related

- [API request shape policy](API_REQUEST_SHAPES.md)
- [Upload and binary data guidelines](UPLOAD_AND_BINARY_DATA_GUIDELINES.md)
- [File and stream benchmark baselines](file-and-stream-benchmark-baselines.md)
