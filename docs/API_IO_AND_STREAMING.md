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

## Decisions not taken

No `IAsyncEnumerable<T>` endpoint, SSE endpoint, WebSocket protocol, SignalR hub, gRPC streaming, or
HTTP/3 mandate exists or is planned. Each needs a named consumer, direction, media type, limits,
cancellation, disconnect, resume, and error semantics, and HTTP/2 or HTTP/3 needs evidence through the
complete proxy and load-balancer path with fallback retained.

## Related

- [API request shape policy](API_REQUEST_SHAPES.md)
- [File and stream benchmark baselines](file-and-stream-benchmark-baselines.md)
