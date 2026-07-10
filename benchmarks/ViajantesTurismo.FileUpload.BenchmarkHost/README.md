# ViajantesTurismo.FileUpload.BenchmarkHost

Minimal Kestrel-only host for real multipart upload scanning benchmarks.

## Endpoints

- `GET /health`: readiness probe for wrappers and k6 setup.
- `POST /upload/scan`: reads multipart file section `file`, scans every byte in the request stream,
  and returns `files=<count>;bytes=<bytes>;matches=<matches>` as `text/plain`.

## Run

Use the repository-owned .NET performance tool so the host starts on an ephemeral loopback Kestrel port
and k6 receives the real base URL:

```bash
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- file-upload-scan
```

Raw two-terminal flow:

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost/ViajantesTurismo.FileUpload.BenchmarkHost.csproj -c Release -- --urls http://127.0.0.1:5009
VT_UPLOAD_BASE_URL=http://127.0.0.1:5009 k6 run tests/performance/k6/scenarios/file-upload-scan.js
```

Results and host logs from the tool are written under the ignored `tests/performance/results/`
path.

Docker mode is explicit opt-in (`VT_K6_USE_DOCKER=1`). In that mode the tool binds the host to
`0.0.0.0` only so the pinned k6 container can reach it through `host.docker.internal`.
