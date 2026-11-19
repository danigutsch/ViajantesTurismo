# Upload and binary data guidelines

Repository guideline for code that receives, parses, stores, serves, or logs uploads, streams, and binary data.
It is the long form of the [`io-streams` skill](../.agents/skills/io-streams/SKILL.md), which is the agent checklist.
Current flows and measurements live in [API I/O and streaming contracts](API_IO_AND_STREAMING.md); this page holds the rules.

Every rule says where it comes from. "Project policy" means a decision of this repository with no external source behind
it; it is a choice, not a fact about the platform. See [Unverified or project-policy items](#unverified-or-project-policy-items).

## Scope

- Applies to: request bodies and multipart files, Blazor browser file reads, CSV and image intake, anything that reads or
  writes a `Stream`, `byte[]`, `ReadOnlyMemory<byte>`, or span, hand-written binary parsers, bytes stored in PostgreSQL or
  object storage, bytes served to browsers, and bytes carried in integration events.
- Does not apply to: JSON or text payloads that have no binary part, HTTP progress UI, and realtime transports
  (see [Decisions not taken](API_IO_AND_STREAMING.md#decisions-not-taken)).
- Reusable stream primitives (`PandaBear.IO.SeekableContent`, the IO analyzers) live in the external SharedKernel
  repository. This page describes only what the pinned packages and this repository show.

## Rules

| Rule | Why | Source or project policy | Where the repo does it |
| --- | --- | --- | --- |
| Allow-list file types; never trust the declared `Content-Type` or the client file name | Both are attacker controlled | [OWASP File Upload](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html), [Microsoft file uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0) | `src/ViajantesTurismo.Branding.Infrastructure/BrandingLogoIntake.cs` accepts only PNG, JPEG, WebP; the declared `Content-Type` is parsed as a media type and parameters such as `charset` are ignored |
| Confirm the type by signature, never by signature alone; content must match the claimed type | A signature proves a prefix, not that the file is a valid image | OWASP File Upload; [ASVS v5 V5.2.2](https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x14-V5-File-Handling.md) | `BrandingLogoIntake.CheckStructure` validates the detected format, then `Reencode` decodes it |
| Set the request size limit per endpoint to the smallest viable value | Kestrel defaults to 30,000,000 bytes; the multipart limit is 128 MB | [Kestrel options](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-10.0) | `RequestSizeLimitAttribute` in `src/ViajantesTurismo.Branding.AspNetCore/BrandingEndpoints.cs` |
| Count received bytes while reading; `Content-Length`, `IFormFile.Length`, and `Stream.Length` are hints | A declared length is a claim | Project policy | `SeekableContent.Create` with a byte limit in `BrandingLogoIntake`, `MediaImageUploadIntake`, `CustomerImportEndpoints.ReadCsv` |
| Reject images above a maximum pixel size and bound decode resources | Decompression and pixel bombs exhaust CPU and memory | ASVS v5 V5.2.6; [ImageMagick security policy](https://imagemagick.org/security-policy/) | `ImageProcessingLimits` in `BrandingLogoIntake` (2048 x 2048, 4,000,000 pixels) and `MediaImageUploadIntake` |
| Re-encode images to an allowed format | Strips metadata and defeats polyglot files | OWASP File Upload | `BrandingLogoIntake.Reencode` re-encodes to WebP of at most 40 KiB |
| Scan for malware before decoding into domain state, storing, or serving; fail closed | An unscanned or skipped file is not known clean | [ASVS v5 V5.4.3](https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x14-V5-File-Handling.md), [ClamAV scanning](https://docs.clamav.net/manual/Usage/Scanning.html); refusal on error, timeout, oversize, or disconnect is project policy | `BrandingLogoIntake.Scan` (rejected is 400, unavailable is 503); `CustomerImportEndpoints.ReadCsv` |
| Use server-generated storage names; ignore client path information | Prevents traversal and overwrite | OWASP File Upload; ASVS v5 V5.3.1 to V5.3.3 | Logos are addressed by the SHA-256 of the re-encoded bytes (`BrandingLogo.FromEncodedBytes`) |
| Check every `Stream.Read` result; use `ReadExactly`/`ReadAtLeast` | A read may return fewer bytes than asked before the end | [Stream.Read](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.read?view=net-10.0), [CA2022](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2022) | `ReadExactlyAsync` in `MediaImageUploadIntake` |
| Do length arithmetic in `long`, compare to the remaining length before seeking or allocating, bound every loop, fail closed | Integer overflow and length inconsistency are the usual parser faults | [CWE-190](https://cwe.mitre.org/data/definitions/190.html), [CWE-130](https://cwe.mitre.org/data/definitions/130.html); the concrete rule set is project policy | See [Safe hand-written binary parsers](#safe-hand-written-binary-parsers) |
| Pass the request `CancellationToken` to every read, scan, decode, and store call | A disconnected client must stop the work | Project policy | `ct` flows through `BrandingLogoIntake.Process` |
| Dispose every lease and opened stream on every path | A lease may hold a spooled temporary file | Project policy | `await using var ownedLease` in the three intakes |
| Serve user content with `X-Content-Type-Options: nosniff` | The browser then honours the declared type | [MDN nosniff](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/X-Content-Type-Options) | `BrandingHttpCache.SetImmutableHeaders` |
| Serve content-addressed bytes with a long immutable cache and a strong ETag | The address changes when the bytes change | [RFC 8246](https://www.rfc-editor.org/rfc/rfc8246.html#section-2), [RFC 9110 §8.8.3](https://www.rfc-editor.org/rfc/rfc9110.html#section-8.8.3) | `GetLogo` in `BrandingEndpoints.cs` |
| Validate a content address with an anchored pattern ending in `\z` | `$` also matches before a trailing newline | Project policy | `BrandingLogoPath.IsContentAddress` (`^[0-9a-f]{64}\z`, 100 ms timeout) |
| Refuse SVG | Direct navigation can use SVG's interactive processing mode | [SVG 2 §2.3](https://www.w3.org/TR/SVG2/conform.html#processing-modes-for-svg-sub-resource-documents); refusing SVG is project policy | Allow-lists list raster types only: PNG, JPEG, WebP in Branding; plus AVIF in `MediaUploadValidationOptions` |
| Keep small immutable blobs in their own table; never in list projections | Rows with large values slow every query that touches them | [EF efficient querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying); sizing is project policy | `branding."BrandingLogos"` keyed by hash; settings and revisions keep only the hash |
| Cap the size of bytes carried in events and snapshots | The outbox payload is stored with every revision | Project policy | 40 KiB logo and 64 KiB serialized event caps along the Branding delivery path |
| Never log payload bytes, file names, or temporary paths; override `ToString` on types that hold bytes | Logs outlive requests and reach other systems | [OWASP Logging](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html); the byte rules are project policy | `LogScannerFailure` logs only the failure type |
| Authenticate and authorize uploads and fetches; keep antiforgery on cookie-authenticated form uploads | Uploads are a write path | [Antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) | `RequireAuthorization(BrandingAuthorization.BrandingWrite)` and the mutation rate limit on the logo `PUT` |

## Decision ladder: Stream, ReadOnlyMemory, or Span

There is no Microsoft rule for choosing between these at an API boundary. This ladder is project policy. The language rules
it rests on are sourced: `Span<T>` cannot cross an `await`, and a method that takes `Memory<T>` must not use it after it returns
([Memory and Span usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines)).

1. **`Stream`** where the consumer is a stream API (scanner, image decoder, HTTP body) or the data may be large and spooled
   through SharedKernel `SeekableContent`. The lease owns spooling; the stream must not escape it.
2. **`ReadOnlyMemory<byte>`** for small immutable payloads that are stored or sent (the 40 KiB logo, a finalized document
   artifact). Copy at most once; do not duplicate an array only because an API accepts memory.
3. **Spans** only inside synchronous parsers over a small `stackalloc` header buffer. Cap the size
   ([stackalloc](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc)); never size it from
   untrusted input.
4. **EF rows keep an internal `byte[]`** and convert at the boundary. The pinned EF provider maps `bytea` to `byte[]`.
   The lower-level driver can write `ReadOnlyMemory<byte>` parameters. `BrandingLogoRecord` uses the provider's mapped type
   and exposes `ReadOnlyMemory<byte>` through `BrandingLogo`. [EF mapping][ef-bytea], [Npgsql type mappings][npgsql-types]
5. **Keep public byte payloads as `ReadOnlyMemory<byte>`.** This also avoids CA1819. `System.Text.Json` uses base64 for
   `ReadOnlyMemory<byte>`. `BrandingLogoWireFormatTests` confirms this with the application's source-generated serializer.
   [System.Text.Json converter][json-binary-converter], [wire format tests][branding-wire-format]

Buffer notes: objects of 85,000 bytes or more go to the Large Object Heap, which is collected only in generation 2
([LOH](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap)). `MemoryStream.GetBuffer`
returns the internal array with unused bytes; `ToArray` copies. A rented `ArrayPool<byte>` array is not zeroed and must not be
used after `Return` ([ArrayPool.Return](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return?view=net-10.0)).

The .NET 10 `MemoryStream` uses an `int` capacity and a single backing `byte[]`. Growth is constrained by the maximum
array length and available memory. This is a runtime ceiling, so size the application's buffer budget independently.
[.NET 10 MemoryStream implementation][memory-stream-source]

Use `IncrementalHash.AppendData` and `GetHashAndReset` when an existing bounded loop needs to compute a hash across
chunks. `File.OpenHandle` returns a `SafeFileHandle` for file access APIs such as `RandomAccess`. Dispose the handle
when the file operation ends. [IncrementalHash][incremental-hash], [File.OpenHandle][open-handle]

## Intake order

Required order for an uploaded file:

1. **Signature peek** of a small fixed header (12 bytes in the Branding and Catalog intakes).
2. **Size limit**, enforced while reading, not from a declared length.
3. **Scan** the exact incoming bytes that will be decoded or used as the source for a bounded transform. Persist original
   files only after a clean result on those bytes.
4. **Decode with limits** (dimensions, pixel count, time and memory where the library allows).
5. **Re-encode** to a known format, which strips metadata.
6. **Hash** the stored bytes.
7. **Store**.

Exception rule: bounded header-only parsing that runs before the scan must be documented as an exception at the call site and
in this page, and must stay bounded by the already limited upload size. The signature peek is the standing exception, because
it reads a fixed 12 bytes and decodes nothing.

Branding and Catalog scan the original payload before decoding and persistence. Both currently walk the container before
the scan. Branding calls `ImageSanitizer.Sanitize` in `BrandingLogoIntake.CheckStructure`. Catalog calls
`ImageSanitizer.SanitizeInPlace` on a separate copy in `MediaImageUploadIntake`. Moving these walks after scan approval
remains part of the intake hardening.

When scanning is enabled, scanner unavailability, timeout, oversize or disconnect cannot authorize further processing.
ClamAV's `INSTREAM` protocol returns an error above `StreamMaxLength`. The application's fail-closed responses and cleanup
are policy, covered by Branding intake and real-Kestrel wire tests. The wire tests also compare the captured scan payload
with the submitted image. Branding decodes that input and stores the bounded raster re-encode. The content address hashes
the encoded output. [ClamAV protocol][clamav-protocol], [intake tests][branding-intake-tests], [Kestrel tests][branding-kestrel-tests]

When evaluating ImageMagick limits, the pinned Magick.NET version exposes `ResourceLimits.Width`, `Height`, `Area`, `Memory`,
`Disk`, `MaxMemoryRequest`, `Thread` and `Time`. A pixel cache memory limit can move work to disk. Keep independent limits
on image dimensions, pixel count and spool use. The libwebp 1.3.2 release fixed a lossless decoder vulnerability. Track
security fixes in the codec distributed with the image-processing library. [Magick.NET 15.1.0][magick-resources], [libwebp NEWS][webp-news]

The declared `Content-Type` is parsed as a media type: type and subtype are compared case-insensitively with the allow-list,
parameters such as `charset` are ignored, and an empty, malformed or comma-separated value is refused. The file signature still
has to match the type.

## Safe hand-written binary parsers

Prefer a vetted library. When a hand-written parser is justified, for example to refuse animated containers cheaply, follow these
rules. They are project policy informed by the weakness classes
[CWE-190](https://cwe.mitre.org/data/definitions/190.html) (integer overflow),
[CWE-130](https://cwe.mitre.org/data/definitions/130.html) (length-field inconsistency),
[CWE-20](https://cwe.mitre.org/data/definitions/20.html) (improper input validation),
[CWE-400](https://cwe.mitre.org/data/definitions/400.html) (resource exhaustion), and
[CWE-409](https://cwe.mitre.org/data/definitions/409.html) (highly compressed input).

- Do all length and offset arithmetic in `long`. Never add to a `uint` or `int` and then widen.
- Compare a declared length with the remaining length before assigning `Position`, seeking, or allocating.
- Never allocate from a declared length.
- Bound iteration: cap the number of chunks or the bytes consumed by the already limited upload size.
- Treat truncation and overrun as refusal (fail closed), not as "not animated" or "not matched".
- Never use a catch-all. Catch the specific exception the format can raise, or none.
- Split parsing from seeking: a pure function over a `ReadOnlySpan<byte>` that returns the next step, and a thin loop that does
  the I/O. The pure function is testable without streams.

Format facts used by the animation check:

- PNG: a chunk is length, type, data, CRC; the length must not exceed 2^31-1; an animated PNG has `acTL` before the first `IDAT`
  ([PNG Third Edition](https://www.w3.org/TR/png-3/)).
- WebP (RIFF): animation is set by the `VP8X` animation flag and carried by `ANIM` and `ANMF` chunks; the chunk size excludes the
  header and padding, and odd payloads are padded ([WebP container](https://developers.google.com/speed/webp/docs/riff_container)).

Earlier Branding animation checks returned HTTP 500 for PNG and WebP chunks with invalid lengths. The current
`BrandingLogoIntake.CheckStructure` delegates container validation to SharedKernel `ImageSanitizer` and maps malformed
structures to validation errors. [Branding intake tests][branding-intake-tests]

Hostile-input test list (project policy):

- truncation at every offset of a valid file;
- length fields of 0, the maximum, `int.MaxValue + 1`, and `uint.MaxValue`;
- offset plus length overflow;
- the animation flag set and cleared;
- trailing data after the last chunk;
- zero-byte, exactly-limit, and limit-plus-one bodies;
- a non-seekable stream that returns one byte per `Read`;
- scanner down, timing out, and rejecting;
- cancellation in the middle of a read;
- lease disposal on every path.

## Raw request bodies

A minimal API parameter of type `Stream` or `PipeReader` is `HttpRequest.Body`: not buffered, not rewindable, readable once, and
unusable outside the handler
([parameter binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0)).
The Branding logo `PUT` uses this shape. Required with it:

- an explicit allow-listed content type, checked against the signature;
- `RequestSizeLimitAttribute` and a `SeekableContent` byte limit (both are present: 1 MiB);
- the request `CancellationToken` and bounded reads;
- authorization and a rate limit;
- scanning before the bytes are accepted.

The Branding intake bounds the body through the endpoint limit and the `SeekableContent` byte limit; it does not compare
`Content-Length` with the received length.

## Serving user content

- Send `X-Content-Type-Options: nosniff`.
- Set the download file name yourself in `Content-Disposition` ([MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Disposition));
  HTML-encode any client file name before display or logging. The image endpoints in this repository serve inline images and set
  no `Content-Disposition`; use `attachment` for any new download of non-image content.
- Serve content-addressed bytes with `Cache-Control: public, max-age=31536000, immutable` and the quoted hash as a strong
  `ETag`. For `GET` or `HEAD`, a matching `If-None-Match` uses weak comparison and returns `304` with no content. An
  applicable `304` includes the `ETag` and cache metadata from the `200` response. `GetLogo` supplies the entity tag to
  ASP.NET Core's byte result. [RFC 9110 §13.1.2][if-none-match], [RFC 9110 §15.4.5][not-modified], [RFC 8246][immutable]
- Validate the address (`BrandingLogoPath.IsContentAddress`) before touching the store, and answer `404` for malformed or unknown
  values. The pattern ends in `\z`.
- Admin API and Management Web serve finalized HTML artifacts with `Content-Security-Policy: default-src 'none'; sandbox`,
  an attachment disposition, `nosniff`, and `no-store`. The policy restricts the document and blocks resource loads if
  the HTML renders.
  [CSP sandbox][csp-sandbox], [CSP default-src][csp-default-src]
- Refuse uploaded SVG. SVG 2 restricts image subresources and allows a broader mode for direct navigation. The upload
  allow-lists keep stored user images raster-only. Treat any future SVG sanitizer or offline rasterizer as a separately
  reviewed input path with its own parser limits. [SVG 2 processing modes][svg-modes]

## Storing bytes

- Small immutable blobs (the 40 KiB logo) may live in `bytea` in their own table. Microsoft allows database storage for small
  files and OWASP asks for DBA involvement; this repository resolves the conflict by size: small and capped in the database,
  anything large in object storage.
- Keep blob columns out of list and settings projections; project only the needed columns. Settings and revisions reference the
  logo by hash.
- PostgreSQL moves values above about 2 kB out of line (TOAST) and compresses them; an unchanged TOASTed value is not rewritten on
  `UPDATE`. A per-column `STORAGE EXTERNAL` avoids compressing already compressed images
  ([TOAST](https://www.postgresql.org/docs/current/storage-toast.html)). The repository does not set it.
- Npgsql reads `bytea` as `byte[]` by default and supports `GetStream()` with `CommandBehavior.SequentialAccess` for large values
  ([Npgsql types](https://www.npgsql.org/doc/types/basic.html)).
- PostgreSQL documents a 1 GB field limit. The application uses much smaller per-artifact caps to bound allocations and
  transfers. [PostgreSQL limits][postgres-limits]
- Branding periodically reclaims unreferenced logos after a grace period. Current and historical revision references protect
  their logos. New blob tables need a stated cleanup rule.
- Event and outbox payloads that carry bytes need a size cap enforced by both producer and consumer. Branding sends a complete
  snapshot so the consumer can recover after missed or out-of-order revisions. The producer, outbox, Admin broker queue, and
  consumer enforce a 64 KiB UTF-8 payload limit. See [Branding](branding.md) for measurements and the rollout checks.

## Logging and telemetry

- Log ids, lengths, hash prefixes, and content types. Never log payload bytes, original file names, or temporary paths.
- Sanitize CR and LF and bound the length of any user input that reaches a log message
  ([OWASP Logging](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)).
- Override `ToString` on records that hold byte payloads (project policy). `BrandingLogo` is a record with a `Content` member and
  does not override it today, so do not format the record into a log message.
- Error messages echo no uploaded content.

## Testing

- Prove transport behavior over real Kestrel with `tests/ViajantesTurismo.UploadWire.Testing`, not TestServer. TestServer is not
  evidence of buffering, disconnect, or data-rate behavior.
- Helpers: `LoopbackKestrel` (random loopback port), `GatedChunkContent` and `SlowUpload` (chunks held until the test releases
  them), `RawHttpUpload` (declare a length, send a prefix, abort the connection), `MultipartPayload`, `ScanRecorder` and
  `RecordedScan` (the exact bytes the scanner saw), `RequestLifetimeProbe` with `RequestProbeStartupFilter` (token firing and read
  sizes), and `WireTimeout` (the failure bound for waits).
- Gate concurrency with `TaskCompletionSource` or `SemaphoreSlim`. Never use `Task.Delay` to wait for a server event.
- Wire tests cover Admin customer import, Catalog media upload, and [Branding logo `PUT`][branding-kestrel-tests].
- Use the hostile-input list above for every parser, and the cases in the `io-streams` skill for every intake.

## Blazor client uploads

- The browser may report an empty content type for a file; the server must not depend on it, and the client may use the extension
  only as a hint (project policy). The Branding server rejects an undeclared or unlisted type.
- Pass a real `CancellationToken` tied to the component lifetime to the upload call. `Branding.razor` passes
  `CancellationToken.None` today, which leaves a navigated-away upload running; this is a known gap.
- `IBrowserFile.OpenReadStream(maxAllowedSize)` throws `IOException` when the file is larger than the limit. `Branding.razor` catches
  `IOException` for that case and `InvalidOperationException` for the rest; catch the specific exceptions, not `Exception`.
- Dispose the stream with `await using`, and keep the client limit equal to the server limit (1 MiB for the logo). The client limit
  is a convenience; the server limit is the control.

## Sources

All pages were read on 2026-10-31. Page summaries were produced with a small model, so quotations are paraphrase-grade;
recheck a page before relying on its exact wording.

- <https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html>
- <https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html>
- <https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/en/0x14-V5-File-Handling.md>
- <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0>
- <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-10.0>
- <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0>
- <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.read?view=net-10.0>
- <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2022>
- <https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap>
- <https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return?view=net-10.0>
- <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc>
- <https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines>
- <https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying>
- <https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/X-Content-Type-Options>
- <https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Disposition>
- <https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/sandbox>
- <https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cache-Control>
- <https://developer.mozilla.org/en-US/docs/Web/SVG/Guides/SVG_as_an_image>
- <https://docs.clamav.net/manual/Usage/Scanning.html>
- <https://www.w3.org/TR/png-3/>
- <https://developers.google.com/speed/webp/docs/riff_container>
- <https://cwe.mitre.org/data/definitions/190.html>, <https://cwe.mitre.org/data/definitions/130.html>, <https://cwe.mitre.org/data/definitions/20.html>,
    <https://cwe.mitre.org/data/definitions/400.html>, <https://cwe.mitre.org/data/definitions/409.html>
- <https://imagemagick.org/security-policy/>
- <https://www.postgresql.org/docs/current/storage-toast.html>
- <https://www.npgsql.org/doc/types/basic.html>

## Unverified or project-policy items

Project policy, with no primary source:

- The Stream, `ReadOnlyMemory<byte>`, and span decision ladder.
- Fail-closed application handling and the rule to scan the exact incoming bytes before decoding. See [Intake order](#intake-order)
  for the protocol behavior and test evidence.
- The binary parser rules and the hostile-input test list.
- Refusing SVG in the upload allow-lists. See [Serving user content](#serving-user-content) for the processing-mode distinction.
- Logging rules for byte payloads, hash prefixes, and `ToString` overrides.
- Size caps for blobs in the database and in events.
- Anchoring the content-address pattern with `\z`.

Unverified (recommended, not sourced):

- Fuzzing guidance and the PNG decompression-bomb text.
- `DisableRequestSizeLimit` guidance and "do not dispose `Request.Body` yourself".

[branding-intake-tests]: ../tests/ViajantesTurismo.Branding.InfrastructureTests/BrandingLogoIntakeTests.cs
[branding-kestrel-tests]: ../tests/ViajantesTurismo.Catalog.ApiServiceTests/Branding/BrandingLogoUploadOverKestrelTests.cs
[branding-wire-format]: ../tests/ViajantesTurismo.Branding.InfrastructureTests/BrandingLogoWireFormatTests.cs
[clamav-protocol]: https://docs.clamav.net/manual/Usage/ClamdProtocol.html#instream
[csp-default-src]: https://www.w3.org/TR/CSP3/#directive-default-src
[csp-sandbox]: https://www.w3.org/TR/CSP3/#directive-sandbox
[ef-bytea]: https://github.com/npgsql/efcore.pg/blob/v10.0.3/src/EFCore.PG/Storage/Internal/Mapping/NpgsqlByteArrayTypeMapping.cs
[if-none-match]: https://www.rfc-editor.org/rfc/rfc9110.html#section-13.1.2
[immutable]: https://www.rfc-editor.org/rfc/rfc8246.html#section-2
[incremental-hash]: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.incrementalhash?view=net-10.0
[json-binary-converter]:
https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/ReadOnlyMemoryByteConverter.cs
[magick-resources]: https://github.com/dlemstra/Magick.NET/blob/15.1.0/src/Magick.NET/ResourceLimits.cs
[memory-stream-source]: https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/MemoryStream.cs
[not-modified]: https://www.rfc-editor.org/rfc/rfc9110.html#section-15.4.5
[npgsql-types]: https://www.npgsql.org/doc/types/basic.html
[open-handle]: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.openhandle?view=net-10.0
[postgres-limits]: https://www.postgresql.org/docs/current/limits.html
[svg-modes]: https://www.w3.org/TR/SVG2/conform.html#processing-modes-for-svg-sub-resource-documents
[webp-news]: https://github.com/webmproject/libwebp/blob/v1.3.2/NEWS
