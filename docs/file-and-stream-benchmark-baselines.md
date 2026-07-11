# File and stream benchmark baselines

Epic #694 establishes repeatable baselines for local file scans and real multipart upload scans.

## Design choice

The local file strategy suite uses BenchmarkDotNet because the measured work is isolated, repeatable,
and sensitive to allocation noise.

The upload suite uses a benchmark-only ASP.NET Core Kestrel host under `benchmarks/` plus k6 traffic. It
does not use TestServer as evidence because request-stream buffering and transport behavior are part of
the scenario. It also avoids Aspire AppHost changes because no production database, cache, or service
discovery dependency is needed. A future production endpoint benchmark can use
`WebApplicationFactory<T>.UseKestrel(0)` when the production app itself is the target; this slice keeps a
smaller isolated host and still uses a real Kestrel socket with an ephemeral port and `/health` readiness.
The host binds loopback by default; explicit Docker mode uses `0.0.0.0` only so the pinned k6 container can
reach the host through `host.docker.internal`.

## Benchmark matrix

| Benchmark | Input size | File generation method | Expected metrics | Cleanup rule |
| --- | --- | --- | --- | --- |
| `FileStreamSequential` | 64 KiB, 4 MiB, 64 MiB | Deterministic bytes from `(offset * 31) + 17` written under `BenchmarkDotNet.Artifacts/file-scan-data/` | mean, median, p95, allocations, `MiB/s` | `[GlobalCleanup]` deletes the generated file and removes the data directory when empty |
| `BufferedFileStreamSequential` | 64 KiB, 4 MiB, 64 MiB | Same generated local file as the baseline | mean, median, p95, allocations, `MiB/s` | Same BenchmarkDotNet cleanup |
| `MemoryMappedFileSequential` | 64 KiB, 4 MiB, 64 MiB | Same generated local file as the baseline | mean, median, p95, allocations, `MiB/s` | Same BenchmarkDotNet cleanup |
| `RandomAccessSequential` | 64 KiB, 4 MiB, 64 MiB | Same generated local file as the baseline | mean, median, p95, allocations, `MiB/s` | Same BenchmarkDotNet cleanup |
| repeated scan variants | `ScanCount` 1 and 8 for every strategy and size | Same generated local file per case | same metrics; `MiB/s` multiplies bytes by `ScanCount` | Same BenchmarkDotNet cleanup |
| `file-upload-scan.js` k6 scenario | default 256 KiB multipart payload, override with `VT_UPLOAD_PAYLOAD_BYTES` up to 16 MiB | deterministic k6 string payload sent as multipart field `file` | k6 `http_req_duration`, `http_reqs`, `http_req_failed`, checks, and `file_upload_bytes` counter/rate | .NET tool writes summaries and host logs under ignored `tests/performance/results/` |

## Memory-mapping caveats

`MemoryMappedFile` is included only where a local, seekable file exists. It is unsuitable as a default for:

- HTTP request streams and other live network streams because they are not memory-mappable files.
- small one-pass reads where map creation and view setup can cost more than normal stream reads.
- remote, cloud-backed, compressed, encrypted, or virtualized files where page-fault behavior may hide
  storage latency rather than measure application scan cost.
- high-churn temporary files where mapping lifetime and file deletion semantics complicate cleanup.

Use the stream benchmark results to choose a baseline scanner for request streams. Treat memory mapping as
a local-file strategy only after the input is already materialized as a file and repeated or random access
is a proven need.

## Commands

Run local file scanning benchmarks:

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileScanning.Benchmarks/ViajantesTurismo.FileScanning.Benchmarks.csproj -c Release -- --filter *FileScan*
```

Run real Kestrel upload scanning with k6:

```bash
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- file-upload-scan
```

Raw two-terminal upload flow:

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost/ViajantesTurismo.FileUpload.BenchmarkHost.csproj -c Release -- --urls http://127.0.0.1:5009
VT_UPLOAD_BASE_URL=http://127.0.0.1:5009 k6 run tests/performance/k6/scenarios/file-upload-scan.js
```

Useful overrides:

- `VT_K6_PROFILE=smoke|average-load|stress`
- `VT_K6_VUS=<count>`
- `VT_K6_DURATION=<duration>`
- `VT_UPLOAD_PAYLOAD_BYTES=<bytes>`
- `VT_K6_RESULTS_DIR=<tests/performance/results-subfolder>`
- `VT_K6_USE_DOCKER=0|1`

Security defaults:

- local `k6` is preferred; Docker requires `VT_K6_USE_DOCKER=1`
- Docker image references must be digest-pinned and are run with `--pull=never`
- Docker mounts `tests/performance/k6` read-only and writes only under the ignored results folder
- result paths must stay under `tests/performance/results`
- local k6 receives a minimal process environment plus explicit `-e` values only
- remote k6 modules, external upload targets, custom outputs, `--http-debug`, and host environment
  forwarding require explicit review or opt-in
