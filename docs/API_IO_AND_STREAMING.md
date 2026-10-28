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
- Not measured: ASP.NET Core multipart buffering before the endpoint runs, native Magick.NET memory, peak concurrent
  requests, and the SharedKernel S3 provider. Real HTTP evidence so far is only the isolated upload-scan benchmark in
  [the benchmark baselines](file-and-stream-benchmark-baselines.md).

Decision: retain the single-buffer design. The proven gap is repeated CSV parsing, not buffering, and it is tracked
separately from the stream design. Revisit
`PandaBear.IO.SeekableContent` for media intake if the media maximum grows, if concurrent-upload memory becomes a
measured problem, or if a real-host measurement shows buffering before the endpoint dominates. Preserve the size
checks, the scan-before-parse order for CSV, actual-length comparison, and cancellation in any later change.

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
- [File and stream benchmark baselines](file-and-stream-benchmark-baselines.md)
