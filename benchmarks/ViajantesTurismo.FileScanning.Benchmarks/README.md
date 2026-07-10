# ViajantesTurismo.FileScanning.Benchmarks

BenchmarkDotNet baselines for local file scanning strategies.

## Run

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileScanning.Benchmarks/ViajantesTurismo.FileScanning.Benchmarks.csproj -c Release -- --filter *FileScan*
```

The suite generates deterministic input files under `BenchmarkDotNet.Artifacts/file-scan-data/` and
removes them during benchmark cleanup. BenchmarkDotNet result artifacts stay under the ignored
`BenchmarkDotNet.Artifacts/` path.

## Scope

- default `FileStream` sequential scans
- `BufferedStream` over `FileStream` sequential scans
- `MemoryMappedFile` view-stream scans where a local seekable file exists
- `RandomAccess.Read` offset scans
- one-pass and repeated scans through the `ScanCount` parameter
- allocation reporting through BenchmarkDotNet `MemoryDiagnoser`
- throughput reporting through the custom `MiB/s` column

See [file and stream benchmark baselines](../../docs/file-and-stream-benchmark-baselines.md) for the
design matrix and memory-mapping caveats.
