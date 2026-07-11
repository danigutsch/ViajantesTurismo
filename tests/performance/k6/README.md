# k6 Performance Testing

This folder contains the first performance/load testing implementation for this repository.

## Scenario

- `scenarios/admin-smoke.js`: small Admin API smoke/repro scenario
- `scenarios/file-upload-scan.js`: real Kestrel multipart upload scan scenario

## Supported Profiles

The scenarios support these profile names through `VT_K6_PROFILE`. Each scenario owns its exact VU and
duration defaults in code:

- `admin-smoke.js`: `smoke` is 1 VU for 30 seconds, `average-load` is 5 VUs for 2 minutes,
  and `stress` is 15 VUs for 5 minutes.
- `file-upload-scan.js`: `smoke` is 1 VU for 20 seconds, `average-load` is 5 VUs for 1 minute,
  and `stress` is 10 VUs for 3 minutes.

Each profile carries versioned thresholds beside the scenario code or in shared local modules.
Smoke is strict and short-lived; stress allows wider latency and error tolerance because it is manual
investigation tooling.

The `smoke` and `average-load` profiles are intended for:

- local repeatable verification
- lightweight reliability investigation
- support work for flaky system-test investigation

The `stress` profile is manual-only for now. These assets are not intended for:

- soak testing
- CI release gates

## Prerequisites

1. Start the local stack separately.
2. Ensure the Admin API is reachable.
3. Install `k6` locally when running scenarios. Docker mode is explicit opt-in only.

Install `k6` at user/system level, not as a repo-local vendored binary:

- macOS: `brew install k6`
- Windows: `winget install k6 --source winget`
- Linux: follow the official install guide: <https://grafana.com/docs/k6/latest/set-up/install-k6/>

For hermetic local runs without a host install, use `VT_K6_USE_DOCKER=1`; the repository tool uses a
digest-pinned image and `--pull=never`.

## Required environment

- `VT_API_BASE_URL`: base URL of the Admin API, from Aspire output or the Aspire dashboard

Optional overrides:

- `VT_K6_PROFILE`: defaults to `smoke`
- `VT_K6_VUS`: override VU count
- `VT_K6_DURATION`: override duration for duration-based profiles
- `VT_K6_RESULTS_DIR`: relative output folder for k6 summary JSON, defaults to
  `tests/performance/results` and must stay under that ignored folder
- `VT_K6_USE_DOCKER`: `0` (default local k6) or `1` (explicit Docker k6)
- `VT_K6_DOCKER_IMAGE`: Docker image used in Docker mode; must be pinned by digest
- `VT_UPLOAD_BASE_URL`: base URL of the benchmark upload host for `file-upload-scan.js`
- `VT_UPLOAD_PAYLOAD_BYTES`: deterministic multipart payload size for `file-upload-scan.js`, defaults to `262144`
- `VT_K6_ALLOW_EXTERNAL`: set to `1` only when intentionally running the upload scenario outside the
  local benchmark host
- `VT_K6_ALLOW_HTTP_DEBUG`: set to `1` only for local redacted debugging when passing `--http-debug`
- `VT_K6_ALLOW_REMOTE_OUTPUT`: set to `1` only after reviewing any custom `--out` destination and credentials

## Run with repository runners

```bash
VT_API_BASE_URL=<admin-api-url> dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- admin-smoke
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- file-upload-scan
```

On Windows PowerShell:

```powershell
$env:VT_API_BASE_URL = '<admin-api-url>'
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- admin-smoke
dotnet run --project tools/ViajantesTurismo.Performance.Tool/ViajantesTurismo.Performance.Tool.csproj -- file-upload-scan
```

The file upload .NET tool starts `benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost` on an ephemeral
Kestrel port, waits for `/health`, and passes the discovered real base URL to k6.

Prefer the .NET performance tool for local and Aspire flows.

## Run with Aspire

Set `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1` before starting AppHost. Aspire adds an opt-in
`admin-performance-smoke` executable resource, waits for the Admin API, injects `VT_API_BASE_URL`,
and writes summaries to `tests/performance/results/` unless `VT_K6_RESULTS_DIR` is set.

The AppHost resource wiring is intentionally kept outside `AppHost.cs` in
`src/ViajantesTurismo.AppHost/PerformanceTestingResourceExtensions.cs` so the AppHost remains a
readable orchestration map.

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

Runner behavior:

- uses local `k6` by default
- uses Docker only when `VT_K6_USE_DOCKER=1`
- exports summary JSON to the ignored `tests/performance/results/` folder by default
- rejects `VT_K6_RESULTS_DIR` outside `tests/performance/results/`
- rewrites `http://127.0.0.1:*`, `http://localhost:*`, `https://127.0.0.1:*`, `https://localhost:*` to `host.docker.internal` in Docker mode
- forwards `VT_K6_VUS` and `VT_K6_DURATION` into Docker mode
- uses a digest-pinned `VT_K6_DOCKER_IMAGE` and runs Docker with `--pull=never`
- mounts `tests/performance/k6` read-only and writes only under `tests/performance/results/`
- blocks host environment forwarding, remote outputs, `--http-debug`, and `--insecure-skip-tls-verify` by default

## Run raw k6

```bash
k6 run -e VT_API_BASE_URL=<admin-api-url> tests/performance/k6/scenarios/admin-smoke.js
k6 run -e VT_UPLOAD_BASE_URL=<upload-benchmark-host-url> tests/performance/k6/scenarios/file-upload-scan.js
```

## Run raw Docker k6

```bash
docker run --rm \
  --add-host host.docker.internal:host-gateway \
  --mount type=bind,source="$(pwd)/tests/performance/k6",target=/k6,readonly \
  --mount type=bind,source="$(pwd)/tests/performance/results",target=/results \
  -w /k6 \
  grafana/k6:0.49.0@sha256:8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd run \
  --no-usage-report \
  -e VT_API_BASE_URL=<docker-reachable-admin-api-url> \
  -e VT_K6_PROFILE=smoke \
  scenarios/admin-smoke.js
```

## Conventions

- keep scenario logic separate from workload/profile configuration
- use local modules for repo-owned helpers
- do not import remote JavaScript modules at runtime from maintained scenarios
- use checks for assertions and thresholds for pass/fail behavior
- keep tags and groups meaningful, but avoid one group per request
- keep smoke profiles simple and reproducible
- keep targets local by default; require explicit opt-in for external endpoints
- avoid logging request/response data unless debugging locally and redacting sensitive values
