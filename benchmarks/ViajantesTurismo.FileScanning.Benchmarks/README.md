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

## CSV parse allocation

`CsvParseBenchmarks` compares the customer import path (decode the received bytes to text, then
`PandaBear.Tabular.CsvDocument.Parse`) with a byte-based parser prototype. The import parses the same text three
times, so the suite also measures that. Setup fails if the prototype yields different rows or fields than the text
parser.

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileScanning.Benchmarks/ViajantesTurismo.FileScanning.Benchmarks.csproj -c Release -- --filter *CsvParse*
```

Short-job result on 2026-10-27 for a 1 MiB, 24-column payload (Linux, .NET 10, noisy; rerun without `--job short`
before quoting):

| Method | Mean | Allocated |
| --- | --- | --- |
| `DecodeThenParse` | 5.4 ms | 9.7 MB |
| `DecodeThenParseThreeTimes` | 16.2 ms | 25.1 MB |
| `ParseBytes` (prototype) | 1.9 ms | 4.5 MB |
