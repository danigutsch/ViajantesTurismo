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
| Customer CSV import (`CustomerImportEndpoints.ReadCsv`) | Browser to API | `ContractConstants.CustomerImportMaxFileBytes` = 1 MiB, enforced while copying; oversize and cancellation tested. Three passes: copy to a `MemoryStream`, malware scan, `ReadToEnd` into a string | Malware scanning needs a seekable stream and the parser needs the whole text | Keep buffering. A 1 MiB file bounds the request at about 1 MiB of bytes plus about 2 MiB of string. |
| Finalized document download (`DocumentEndpoints`, `DocumentsApiClient`, Management Web proxy) | API to browser | Rendered HTML artifact held as `ReadOnlyMemory<byte>`, copied once by `ToArray()` in the endpoint and once by `ReadAsByteArrayAsync` in the client. Measured 49,481 bytes with the three staff-editable fields at 4,000 `<` characters each (the largest escaping); `FinalizeDocumentCommandHandlerTests` fails above 64 KiB. Non-editable fields such as tour name and included services are not at their maximum in this measurement | Artifact is stored and audited as one value | Keep buffering. Revisit if the template gains unbounded content. |

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

Decision: retain the single-buffer design. The proven gap is repeated CSV parsing, not buffering, and it is tracked
separately from the stream design. Revisit
`PandaBear.IO.SeekableContent` for media intake if the media maximum grows, if concurrent-upload memory becomes a
measured problem, or if a real-host measurement shows buffering before the endpoint dominates. These conditions were
checked against the measurement above: the media maximum is unchanged at 5 MiB, concurrent memory scales linearly and
is moderate at 1 to 16 uploads in flight with no errors, so it is not shown to be a problem at realistic concurrency
given the rate limits, and the pre-handler share is mostly transfer rather than an isolated buffering cost. The
decision stands. Preserve the size
checks, the scan-before-parse order for CSV, actual-length comparison, and cancellation in any later change.

## Decisions not taken

No `IAsyncEnumerable<T>` endpoint, SSE endpoint, WebSocket protocol, SignalR hub, gRPC streaming, or
HTTP/3 mandate exists or is planned. Each needs a named consumer, direction, media type, limits,
cancellation, disconnect, resume, and error semantics, and HTTP/2 or HTTP/3 needs evidence through the
complete proxy and load-balancer path with fallback retained.

## Related

- [API request shape policy](API_REQUEST_SHAPES.md)
- [File and stream benchmark baselines](file-and-stream-benchmark-baselines.md)
