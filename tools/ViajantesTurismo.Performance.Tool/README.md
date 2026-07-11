# ViajantesTurismo.Performance.Tool

Repo-owned .NET tool for local performance benchmark automation.

## Commands

Run the Admin API smoke scenario after the local stack is running:

```bash
VT_API_BASE_URL=<admin-api-url> dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- admin-smoke
```

Run the real Kestrel upload-scan benchmark:

```bash
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- file-upload-scan
```

The command starts `benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost` on an ephemeral port, waits for
`/health`, and runs `tests/performance/k6/scenarios/file-upload-scan.js` against that real Kestrel socket.

## Security defaults

- local `k6` is the default execution path
- Docker is explicit opt-in only with `VT_K6_USE_DOCKER=1`
- Docker mode requires an image pinned by digest and runs with `--pull=never`
- Docker mode mounts `tests/performance/k6` read-only and writes only to the ignored results folder
- local `k6` receives a minimal process environment plus explicit `-e` values
- `VT_K6_RESULTS_DIR` must stay under `tests/performance/results`
- `--include-system-env-vars`, `--insecure-skip-tls-verify`, uncontrolled `-e` overrides, and custom
  outputs are blocked by default

Use these opt-ins only after local review:

- `VT_K6_USE_DOCKER=1`: run pinned Docker image instead of local k6
- `VT_K6_ALLOW_HTTP_DEBUG=1`: allow `--http-debug` for local redacted debugging
- `VT_K6_ALLOW_REMOTE_OUTPUT=1`: allow custom k6 outputs after checking destination and credentials
