# k6 Performance Testing

This folder contains the first performance/load testing implementation for this repository.

## Scenario

- `scenarios/admin-smoke.js`: small Admin API smoke/repro scenario; runs as the AppHost `admin-performance-smoke`
  container and signs in with the `k6-smoke` service account
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

For hermetic local runs without a host install, pass `--container`; `sharedkernel-k6` runs the
digest-pinned image from `.config/sharedkernel-k6.json` with `--pull=never`, so pull it first.

## Required environment

- `admin-smoke.js`: the AppHost resource sets `VT_API_BASE_URL` and the `SK_K6_TOKEN_URL`, `SK_K6_TOKEN_HOST`,
  `SK_K6_CLIENT_ID`, and `SK_K6_CLIENT_SECRET` sign-in values. The scripts import `/k6lib/auth.js` and
  `/k6lib/summary.js`, which the AppHost package mounts.

Optional overrides:

- `VT_K6_PROFILE`: defaults to `smoke`
- `VT_K6_VUS`: override VU count
- `VT_K6_DURATION`: override duration for duration-based profiles
- `VT_UPLOAD_PAYLOAD_BYTES`: deterministic multipart payload size for `file-upload-scan.js`, a whole
  number from 1 to 16777216, defaults to `262144`

`sharedkernel-k6` sets `VT_UPLOAD_BASE_URL` for `file-upload-scan.js` from the benchmark host it starts.
An unknown `VT_K6_PROFILE` fails the run.

Options of `sharedkernel-k6 run`:

- `--container`: run the configured digest-pinned image with Docker instead of local `k6`
- `--image <ref@sha256:digest>`: replace the configured image; requires `--container`
- `--allow-external-target`: allow a base URL outside loopback
- `--allow-http-debug`: allow `--http-debug`, only for local redacted debugging
- `--allow-remote-output`: allow `--out` and other k6 outputs, only after reviewing the destination and
  credentials

## Run with repository runners

```bash
dotnet tool run sharedkernel-k6 -- run --scenario file-upload-scan
```

For `file-upload-scan`, `sharedkernel-k6` starts `benchmarks/ViajantesTurismo.FileUpload.BenchmarkHost` on
an ephemeral Kestrel port, waits for `/health`, and passes the discovered real base URL to k6.

## Run with Aspire

Set `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1` before starting AppHost. Aspire adds an opt-in
`admin-performance-smoke` k6 container resource that waits for the Admin API and the identity provider, injects
`VT_API_BASE_URL`, and starts only when you start it from the dashboard or with
`dotnet tool run aspire resource admin-performance-smoke start`. The container mounts `tests/performance/k6`
read-only and writes `tests/performance/results/admin-smoke-summary.json`. The summary holds options, metrics, and
checks; it holds no token or secret. A failed token request or a breached threshold ends the container with a
non-zero exit code.

The AppHost resource wiring is intentionally kept outside `AppHost.cs` in
`src/ViajantesTurismo.AppHost/PerformanceTestingResourceExtensions.cs` so the AppHost remains a
readable orchestration map.

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

`sharedkernel-k6` runner behavior (`file-upload-scan`):

- uses local `k6` with a minimal environment by default, and Docker only with `--container`
- exports summary JSON to a new run directory under the ignored `tests/performance/results/` folder
- rewrites a loopback target to `host.docker.internal` in container mode
- forwards `VT_K6_PROFILE`, `VT_K6_VUS`, `VT_K6_DURATION`, and `VT_UPLOAD_PAYLOAD_BYTES` with `-e` when set
- runs a digest-pinned image with `--pull=never`
- mounts `tests/performance/k6` read-only and writes only to the run directory
- blocks host environment forwarding, remote outputs, `--http-debug`, and `--insecure-skip-tls-verify` by default

## Run raw k6

```bash
k6 run -e VT_UPLOAD_BASE_URL=<upload-benchmark-host-url> tests/performance/k6/scenarios/file-upload-scan.js
```
