---
name: io-streams
description: Design, review, test, and measure ViajantesTurismo upload, import, and stream-handling code. Use for IFormFile or request-body handling, MemoryStream buffering, CopyTo or hand-rolled read loops, non-seekable or slow streams, multi-pass scan, validate, decode, hash, and store flows, PandaBear.IO SeekableContent, request size limits, stream cancellation and disposal, CA2022 or CA2016 findings, and upload benchmarks. Not for HTTP progress UI or realtime transports.
license: See repository LICENSE.txt
---

# I/O and Stream Handling Workflow

Use this skill to change or review code that reads incoming files or streams. The contracts live in the docs below;
this skill is the workflow. The long form of every rule, with sources, is
`docs/UPLOAD_AND_BINARY_DATA_GUIDELINES.md`; items there marked project policy are repository decisions, not platform facts.

## Read first

1. `docs/UPLOAD_AND_BINARY_DATA_GUIDELINES.md`: rules, sources, intake order, safe parsers, serving, storage, logging, testing.
2. `docs/API_IO_AND_STREAMING.md`: current streamed and buffered flows (including the Branding logo), bounds, and decisions not taken.
3. `docs/file-and-stream-benchmark-baselines.md`: BenchmarkDotNet and real-Kestrel k6 commands.
4. `docs/media-image-processing.md` and `docs/sharedkernel/development.md` (candidate and pin workflow).
5. SharedKernel `docs/stream-intake.md`: the `PandaBear.IO.SeekableContent` decision and lease contract. It lives in the external
   SharedKernel repository, not in this one; the pinned package READMEs under `~/.nuget/packages/pandabear.sharedkernel.*/` are the local copy.
6. The nearest `AGENTS.md` for every file you touch.

## Decision ladder

Stop at the first rung that holds.

1. **Measure first.** Count passes, bytes read, rewinds, and retained memory with a counting stream, and use real
   Kestrel (k6 benchmark host) for transport claims. TestServer is not evidence for buffering or transport behavior.
   No production change without a failing test or measured gap.
2. **Keep bounded buffering** when the configured maximum is small and measured safe (customer CSV import is
   1 MiB; see `API_IO_AND_STREAMING.md`).
3. **One forward pass** when validation, scanning, and the destination allow it.
4. **`SeekableContent`** when a non-seekable input needs several independent passes (validate, scan, decode, hash,
   store) and the size could pressure memory. It copies with a hard byte limit, spills past a memory threshold, and is
   a lease: `await using`, rewind (`Position = 0`) before each pass, never let the stream escape the lease.
5. Never memory-map a request stream. Never stream unscanned records into application logic.

## Safety invariants

- Count received bytes while reading. `IFormFile.Length`, `Content-Length`, and `Stream.Length` are hints, not guards.
  Reject a mismatch between declared and actual length.
- Keep the request size limit on the endpoint (`RequestSizeLimitAttribute`). Do not use `DisableRequestSizeLimit`,
  `RequestFormLimits.BufferBody = true`, or unbounded `EnableBuffering()` without a stated bound.
- Malware scan approval comes before parsing, decoding into domain state, or persistence. Exception: bounded header-only
  parsing before the scan (the 12-byte signature peek) is allowed when it is documented as an exception and stays bounded.
  The Branding animation chunk walk runs before the scan today; that is a known deviation tracked in the I/O hardening epic,
  not a pattern to copy.
- Forward the request `CancellationToken` to every read, scan, and store call. Cancellation must clean up partial
  copies and temporary files, and must not leave a half-committed state.
- Dispose every lease and opened stream on every path, including scanner failure and validation failure.
- Preserve auth, media-type and content validation, privacy, and transaction or compensation ordering.
- Do not log payloads, original filenames, temporary paths, or upload credentials.
- Do not duplicate byte arrays merely because an API accepts `ReadOnlyMemory<byte>`; do not add a second copy.
- Do not trust one machine's result to set an application-wide threshold.
- Content type: the declared type and file name are untrusted. Allow-list the type, cross-check it against the signature, and
  re-encode images so the stored bytes are ours.
- Hand-written binary parsers: `long` arithmetic, compare a declared length with the remaining length before seeking or
  allocating, bound iteration, fail closed on truncation, no catch-all, and split parsing (pure span function) from seeking.
- Raw body (`Stream` parameter or `HttpRequest.Body`): not rewindable and readable once. Require a content-type allow-list,
  `RequestSizeLimitAttribute`, a `SeekableContent` byte limit, the request token, authorization, a rate limit, and a scan.
- Serving: `X-Content-Type-Options: nosniff`; `Content-Disposition` set by the server for downloads; content-addressed bytes get
  an immutable cache and a strong ETag; validate the hash format with a `\z`-anchored pattern; refuse SVG.
- Storage: small immutable blobs in their own table with a size cap, never in list projections, with a stated orphan cleanup
  rule (Branding logos are never deleted today). Cap bytes carried in events and outbox payloads on both producer and consumer.
- Logging: ids, lengths, hash prefixes, content types only. No payload bytes, file names, or temporary paths; do not format
  byte-holding records into messages.
- Blazor client: the declared content type may be empty; pass a real cancellation token (not `CancellationToken.None`); catch the
  specific exceptions `OpenReadStream` throws. The server limit is the control, the client limit a convenience.
- `Stream` or `ReadOnlyMemory<byte>`: `Stream` for stream consumers and large or spooled data; `ReadOnlyMemory<byte>` for small
  immutable stored or sent payloads; spans only inside synchronous parsers; EF rows keep `byte[]` internally; no public `byte[]`
  properties.

## Test recipe

- Failing test first (repository rule for audit findings), then the production change.
- Use gated, counting, non-seekable, and slow streams. Use barriers and `TaskCompletionSource`, not sleeps or GC
  thresholds. A non-seekable source exists in `tests/SharedKernel.ImageProcessing.Tests/NonSeekableStream.cs`.
- Cover: exact limit, limit plus one byte, declared versus actual length mismatch, empty input, cancellation mid-read,
  scanner rejection and failure, disposal on every path, and partial-copy cleanup.
- Real HTTP path: `tests/SharedKernel.IntegrationTesting/AspireTestApplication.cs` for disconnect and slow-client cases.
- Hostile parser input: truncation at every offset; length fields 0, max, `int.MaxValue + 1`, `uint.MaxValue`; offset plus length
  overflow; flag set and cleared; trailing data; a stream returning one byte per `Read`; scanner down, timeout, oversize.

Per-feature wire-test checklist (real Kestrel, `tests/ViajantesTurismo.UploadWire.Testing`; no TestServer, no `Task.Delay`):

- `LoopbackKestrel.UseRandomLoopbackPort` and `Start` for a real socket.
- `SlowUpload` with `GatedChunkContent` for slow clients; the scan must start only after the last chunk.
- `RawHttpUpload` for a client that declares a length, sends a prefix, and aborts.
- `ScanRecorder` and `RecordedScan` to assert the exact bytes scanned, or that no scan happened.
- `RequestLifetimeProbe` with `RequestProbeStartupFilter` for token cancellation and read sizes; `WireTimeout.Default` bounds waits.
- Cover over-limit (`413`), over-file-limit, stalled client (`408`), and persistence not happening. Admin customer import and
  Catalog media upload have such tests; the Branding logo `PUT` does not yet.
- Use `docs/TEST_GUIDELINES.md` and the `xunit-v3-mtp-test-stack` skill for runner details.

## Diagnostics to expect

| Diagnostic | Meaning | Fix |
| --- | --- | --- |
| CA2022 | `Stream.Read` return value unchecked | Check the count, or `ReadExactly[Async]` |
| CA2016 | CancellationToken not forwarded | Forward it, or pass `None` deliberately |
| CA2000 | Disposable not disposed | `using` or `await using`; document ownership transfer |
| CA2012 | ValueTask consumed incorrectly | Await directly once |
| CA2024 | `StreamReader.EndOfStream` in async code | Use `ReadLineAsync` returning `null` |
| SKIO001 | Request or upload stream buffered in memory without a length bound (`PandaBear.SharedKernel.IO.Analyzers`) | Use `SeekableContent.Create` with a byte limit |
| SKIO002 | Hand-rolled read loop into a `MemoryStream` without a length bound | Use `SeekableContent.Create` |

`PandaBear.SharedKernel.IO.Analyzers` (SKIO001, SKIO002, both Warning) is shipped and referenced in
`ViajantesTurismo.Admin.ApiService`, `ViajantesTurismo.Catalog.Application`, and `ViajantesTurismo.Catalog.ApiService`. It is
not referenced in the Branding projects, which hold the raw-body logo intake; adding it there is a candidate follow-up. The
analysis is local: it does not see host-configured request limits or follow parameters. Treat warnings as errors; do not suppress
without a written justification.

## Workflow

1. Trace the flow end to end: where it is read, copied, rewound, scanned, hashed, decoded, stored. Note each size guard
   and keep it.
2. Choose a rung of the decision ladder from measurements.
3. Write the failing test. Change one flow per slice.
4. Run focused tests (`dotnet test --project <csproj>` with `--filter-class`), then the solution tests.
5. Re-run the benchmark and record before and after. If the current design stays, say so in the docs.
6. A reusable change belongs in SharedKernel and needs a new candidate and application compatibility validation
   before adoption (`docs/sharedkernel/development.md`).
