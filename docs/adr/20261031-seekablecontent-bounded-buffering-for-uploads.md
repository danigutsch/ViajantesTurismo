# ADR-045: SeekableContent as the Bounded Buffering Pattern for Uploads

**Status**: Accepted — 2026-10-31

## Context

The upload and import flows scan, validate, decode, hash and store the same bytes in several independent passes. The
request body is a one-shot stream, and `IFormFile.OpenReadStream()` may already be buffered by the framework. The
decision to buffer through `PandaBear.IO.SeekableContent` was recorded only inside
[API I/O and streaming contracts](../API_IO_AND_STREAMING.md), together with the measurements and the decisions not
taken. This ADR records the decision on its own so it can be found and superseded.

Three production intakes use it today:

| Flow | Entry point | Byte limit | Memory spool threshold |
| --- | --- | --- | --- |
| Branding logo upload | `BrandingLogoIntake.Process` | `BrandingLogoLimits.MaxUploadBytes` (1 MiB) | `BrandingLogoLimits.SpoolMemoryThresholdBytes` (256 KiB) |
| Catalog media upload | `MediaImageUploadIntake.Accept` | `MediaUploadValidationOptions.MaxLengthBytes` | `SpoolMemoryThresholdBytes` (1 MiB) |
| Customer CSV import | `CustomerImportEndpoints.ReadCsv` | `ContractConstants.CustomerImportMaxFileBytes` (1 MiB) | equal to the file maximum (1 MiB) |

## Decision

Every upload or import that needs more than one pass over the received bytes takes them through
`SeekableContent.Create(input, maxLength, memoryThreshold, ct)` and holds the result as a lease.

- **Bounded memory.** Actual received bytes enforce the maximum; excess throws `InvalidDataException`, which the
  intakes map to an invalid-input result. `Content-Length`, `IFormFile.Length` and `Stream.Length` are hints, not guards.
- **Spool above a threshold.** Input that has to be copied stays in memory up to the threshold and then moves to a
  private temporary file. The threshold is chosen per flow. The 256 KiB figure is the Branding logo threshold; media
  uses 1 MiB, and the CSV import sets it equal to its 1 MiB maximum, so it never spools to disk.
- **Multi-pass on the same bytes.** Signature check, malware scan, chunk walk, decode, hash and store all read the
  lease's one `Content` stream, and each pass rewinds (`Position = 0`) first. A seekable source at position zero
  (typically `IFormFile.OpenReadStream()`) is counted once and returned uncopied; anything else is copied once.
- **Lease discipline.** Dispose the lease on every path (`await using`). The lease never disposes the caller's input,
  so the endpoint keeps owning `IFormFile.OpenReadStream()`. The stream does not escape the lease; the CSV flow
  allocates one array after the scanner approves, because its result outlives the stream.
- Hand-rolled copy loops into a `MemoryStream` and unbounded request buffering are flagged by `SKIO001` and `SKIO002`
  (`PandaBear.SharedKernel.IO.Analyzers`).

The primitive lives in the SharedKernel repository, pinned at `PandaBear.SharedKernel.IO` 0.1.0-alpha.425.1
(`Directory.Packages.props`). This repository does not carry a copy.

## Alternatives

Measurements come from [API I/O and streaming contracts](../API_IO_AND_STREAMING.md); they are indicative
single-machine scratch results on a shared WSL2 host with no Magick.NET decode, real scanner, object store, rate limiter
or authentication. They are not production budgets.

- **Plain `MemoryStream` copy (the previous intake).** Retained memory is bounded by the configured maximum, but each
  upload is a second copy of a stream the framework already buffered, and the buffer lands on the large object heap.
  Measured 5 MiB uploads at 64 in flight: 10.21 GB allocated, 53 gen2 collections, 2.34 s GC pause, 900 MB peak RSS.
  The `SeekableContent` run: 0.67 GB, 1 gen2 collection, 0.20 s, 220 MB. At 1 in flight the new pattern was slower in
  the median (18.7 against 21.7 requests per second) because of the extra counting pass. The first version of the
  primitive always copied and was not adopted for that reason; the pinned version passes a seekable source through.
- **Direct reads from `IFormFile.OpenReadStream()` without a lease.** The scratch experiment gave the same direction
  (allocation 5.95 GB to 0.34 GB at 16 in flight). Rejected as a pattern because it depends on the framework having
  buffered the file, gives no byte-count guard of its own, and cannot serve a non-seekable source such as the raw
  Branding request body. `SeekableContent` yields the same pass-through for a seekable source and a bounded
  spool otherwise.
- **Memory-mapping.** Not used and not measured for request streams. The benchmark baselines document that
  `MemoryMappedFile` needs a local seekable file, is unsuitable for HTTP request streams and small one-pass reads, and
  complicates cleanup of high-churn temporary files; it stays a local-file strategy after a proven need. The `io-streams`
  skill states the rule: never memory-map a request stream.
- **One forward pass.** Preferred when validation, scanning and the destination allow it. These flows cannot: the
  scanner, decoder and hash each need the bytes, and the scan must precede parsing.

## Consequences

- Retained content per request is bounded by the configured limit, and anything past the threshold goes to disk. The
  threshold bounds retained content, not all process allocations (SharedKernel README).
- The CSV import gains consolidation (one tested bounded read instead of a private copy loop) but no allocation saving:
  it still allocates one content-sized array. Its measured gap is repeated parsing, not buffering.
- At low concurrency a counting pass costs some throughput, as measured above.
- Spooled content is plaintext on disk for the lease lifetime. The SharedKernel README assigns request admission, private
  temporary storage, disk quotas and crash cleanup to the host. Logs must not contain payloads or temporary paths.
- Concurrency is not limited in this repository: the rate limits cap request rate per client IP, not in-flight uploads,
  and no concurrency limiter or container memory limit was found.
- Native Magick.NET memory and the SharedKernel S3 provider were not measured. No Branding wire test over real Kestrel
  exists yet; the disconnect, slow-client and data-rate behavior of the raw-body logo `PUT` is not covered.
- A new multi-pass upload flow uses `SeekableContent` with a byte limit and keeps the existing guards: size limit,
  scan-before-parse, actual-length comparison, cancellation and the non-seekable path. Changing the threshold or the
  pattern needs a measured gap and a new ADR.

## Limits and unverified items

- UNVERIFIED: the temporary file directory and the crash-time cleanup behavior. The pinned README and XML docs say
  only that content spills to a "private temporary file deleted on disposal" and that hosts own private temporary
  storage, disk quotas and crash cleanup. The package source and the SharedKernel stream intake policy are not in this
  repository, so the exact location and sweep behavior are not confirmed here.
- UNVERIFIED: the 256 KiB figure as a general default. It is the Branding setting only; the other intakes configure 1 MiB.
- Measurements are scratch-host, single-machine, with no production concurrency data.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [API I/O and streaming contracts](../API_IO_AND_STREAMING.md)
- Related: [Upload and binary data guidelines](../UPLOAD_AND_BINARY_DATA_GUIDELINES.md)
- Related: [File and stream benchmark baselines](../file-and-stream-benchmark-baselines.md)
- Related: [io-streams skill](../../.agents/skills/io-streams/SKILL.md)
- Related: [SharedKernel pin and candidate workflow](../sharedkernel/development.md)
