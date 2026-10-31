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
| Finalized document download (`DocumentEndpoints`, `DocumentsApiClient`, Management Web proxy) | API to browser | Rendered HTML artifact held as `ReadOnlyMemory<byte>`, copied once by `ToArray()` in the endpoint and once by `ReadAsByteArrayAsync` in the client. Measured 49,481 bytes with the three staff-editable fields at 4,000 `<` characters each (the largest escaping); `FinalizeDocumentCommandHandlerTests` fails above 64 KiB. Non-editable fields such as tour name and included services are not at their maximum in this measurement | Artifact is stored and audited as one value | Keep buffering. Revisit if the template gains unbounded content. |
| Branding logo upload (`BrandingLogoIntake.Process`, `PUT /api/v1/branding/settings/logo`) | Browser to API | `BrandingLogoLimits.MaxUploadBytes` = 1 MiB, enforced by `RequestSizeLimitAttribute` and by the `SeekableContent` byte limit; 256 KiB memory spool threshold. Raw request body, not multipart | The signature check, malware scan, animation chunk walk, and decode need several seekable passes | Keep the lease. See [Branding logo flow](#branding-logo-flow). |

Any change to these flows must keep malware scanning, the size limit, authorization, audit, privacy, and
`no-store` headers.

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
- The local object store writes with one `CopyToAsync` into a temporary file and moves it into place (code reading).
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
8. `GET /api/v1/public/branding/logo/{sha256}` serves it with a long `immutable` cache, a strong `ETag`, and `nosniff`; the hash
   is validated with an anchored pattern before the store is read.

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
| Stalled client below a minimum body data rate | `408` from Kestrel; no scan, nothing persisted |

No Branding wire test over real Kestrel exists yet, so disconnect, slow-client, over-limit, and data-rate behavior of the raw-body
logo `PUT` are not covered by the table above. Branding tests cover the intake, the path validation, and the wire format of the
event. Adding a Branding wire test with `tests/ViajantesTurismo.UploadWire.Testing` is tracked in the I/O hardening epic.

Timeouts: the Admin and Catalog API hosts configure no `MinRequestBodyDataRate`, `RequestHeadersTimeout`, or
`KeepAliveTimeout` (only `UseKestrelHttpsConfiguration`), so Kestrel framework defaults apply. The `408` test sets
`MinRequestBodyDataRate` on the test server only, to prove the endpoints abort the read and never scan when a rate is
enforced. It does not claim production configures one.

## Decisions not taken

No `IAsyncEnumerable<T>` endpoint, SSE endpoint, WebSocket protocol, SignalR hub, gRPC streaming, or
HTTP/3 mandate exists or is planned. Each needs a named consumer, direction, media type, limits,
cancellation, disconnect, resume, and error semantics, and HTTP/2 or HTTP/3 needs evidence through the
complete proxy and load-balancer path with fallback retained.

## Related

- [API request shape policy](API_REQUEST_SHAPES.md)
- [Upload and binary data guidelines](UPLOAD_AND_BINARY_DATA_GUIDELINES.md)
- [File and stream benchmark baselines](file-and-stream-benchmark-baselines.md)
