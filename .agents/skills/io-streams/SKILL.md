---
name: io-streams
description: Design, review, test, and measure ViajantesTurismo upload, import, and stream-handling code. Use for IFormFile or request-body handling, MemoryStream buffering, CopyTo or hand-rolled read loops, non-seekable or slow streams, multi-pass scan, validate, decode, hash, and store flows, PandaBear.IO SeekableContent, request size limits, stream cancellation and disposal, CA2022 or CA2016 findings, and upload benchmarks. Not for HTTP progress UI or realtime transports.
license: See repository LICENSE.txt
---

# I/O and Stream Handling Workflow

Use this skill to change or review code that reads incoming files or streams. The contracts live in the docs below;
this skill is the workflow. It adds no rules of its own.

## Read first

1. `docs/API_IO_AND_STREAMING.md`: current streamed and buffered flows, bounds, and decisions not taken.
2. `docs/file-and-stream-benchmark-baselines.md`: BenchmarkDotNet and real-Kestrel k6 commands.
3. `docs/media-image-processing.md` and `docs/sharedkernel/development.md` (candidate and pin workflow).
4. SharedKernel `docs/stream-intake.md`: the `PandaBear.IO.SeekableContent` decision and lease contract.
5. The nearest `AGENTS.md` for every file you touch.

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
- Malware scan approval comes before parsing, decoding into domain state, or persistence.
- Forward the request `CancellationToken` to every read, scan, and store call. Cancellation must clean up partial
  copies and temporary files, and must not leave a half-committed state.
- Dispose every lease and opened stream on every path, including scanner failure and validation failure.
- Preserve auth, media-type and content validation, privacy, and transaction or compensation ordering.
- Do not log payloads, original filenames, temporary paths, or upload credentials.
- Do not duplicate byte arrays merely because an API accepts `ReadOnlyMemory<byte>`; do not add a second copy.
- Do not trust one machine's result to set an application-wide threshold.

## Test recipe

- Failing test first (repository rule for audit findings), then the production change.
- Use gated, counting, non-seekable, and slow streams. Use barriers and `TaskCompletionSource`, not sleeps or GC
  thresholds. A non-seekable source exists in `tests/SharedKernel.ImageProcessing.Tests/NonSeekableStream.cs`.
- Cover: exact limit, limit plus one byte, declared versus actual length mismatch, empty input, cancellation mid-read,
  scanner rejection and failure, disposal on every path, and partial-copy cleanup.
- Real HTTP path: `tests/SharedKernel.IntegrationTesting/AspireTestApplication.cs` for disconnect and slow-client cases.
- Use `docs/TEST_GUIDELINES.md` and the `xunit-v3-mtp-test-stack` skill for runner details.

## Diagnostics to expect

| Diagnostic | Meaning | Fix |
| --- | --- | --- |
| CA2022 | `Stream.Read` return value unchecked | Check the count, or `ReadExactly[Async]` |
| CA2016 | CancellationToken not forwarded | Forward it, or pass `None` deliberately |
| CA2000 | Disposable not disposed | `using` or `await using`; document ownership transfer |
| CA2012 | ValueTask consumed incorrectly | Await directly once |
| CA2024 | `StreamReader.EndOfStream` in async code | Use `ReadLineAsync` returning `null` |

Planned SharedKernel analyzers (not shipped; update this table when they are): unbounded `MemoryStream` buffering of
request streams, hand-rolled copy loops that should use `SeekableContent`, and endpoints that bind uploads without a
request size limit. Treat warnings as errors; do not suppress without a written justification.

## Workflow

1. Trace the flow end to end: where it is read, copied, rewound, scanned, hashed, decoded, stored. Note each size guard
   and keep it.
2. Choose a rung of the decision ladder from measurements.
3. Write the failing test. Change one flow per slice.
4. Run focused tests (`dotnet test --project <csproj>` with `--filter-class`), then the solution tests.
5. Re-run the benchmark and record before and after. If the current design stays, say so in the docs.
6. A reusable change belongs in SharedKernel and needs a new candidate and application compatibility validation
   before adoption (`docs/sharedkernel/development.md`).
