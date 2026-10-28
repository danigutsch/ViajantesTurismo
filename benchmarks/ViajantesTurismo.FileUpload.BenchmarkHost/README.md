# ViajantesTurismo.FileUpload.BenchmarkHost

Minimal Kestrel-only host for real multipart upload scanning benchmarks.

## Endpoints

- `GET /health`: readiness probe for wrappers and k6 setup.
- `POST /upload/scan`: reads multipart file section `file`, scans every byte in the request stream,
  and returns `files=<count>;bytes=<bytes>;matches=<matches>` as `text/plain`.
- `POST /upload/scan-buffered`: production-shaped intake (`IFormFile`, presized `MemoryStream`, one scan, one SHA-256);
  same response plus `;sha256=<hash>`. Select it in k6 with `VT_UPLOAD_MODE=buffered`.
- `POST /upload/scan-seekable`: the adopted intake shape (`IFormFile`, `PandaBear.IO.SeekableContent` pass-through, one scan, one
  SHA-256); same response as buffered. Select it with `VT_UPLOAD_MODE=seekable`.
- `GET /diagnostics`: process allocation, GC collection counts and total GC pause, for deltas around a run.

## Run

Run the `file-upload-scan` scenario of `sharedkernel-k6` so the host starts on an ephemeral loopback
Kestrel port and k6 receives the real base URL:

```bash
dotnet tool run sharedkernel-k6 -- run --scenario file-upload-scan
```

Raw two-terminal flow:

```bash
dotnet run --project benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost/ViajantesTurismo.FileUpload.BenchmarkHost.csproj -c Release -- --urls http://127.0.0.1:5009
VT_UPLOAD_BASE_URL=http://127.0.0.1:5009 k6 run tests/performance/k6/scenarios/file-upload-scan.js
```

Results and host logs from the tool are written under the ignored `tests/performance/results/`
path.

Container mode is explicit opt-in (`--container`). In that mode the tool binds the host to
`0.0.0.0` only so the pinned k6 container can reach it through `host.docker.internal`.
