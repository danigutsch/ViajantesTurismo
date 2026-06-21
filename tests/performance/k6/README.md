# k6 Performance Testing

This folder contains the first performance/load testing implementation for this repository.

## Scenario

- `scenarios/admin-smoke.js`: small Admin API smoke/repro scenario

## Supported Profiles

The scenario supports these profiles through `VT_K6_PROFILE`:

- `smoke`: default, 1 VU for 30 seconds
- `average-load`: conservative regular validation, 5 VUs for 2 minutes
- `stress`: manual investigation, 15 VUs for 5 minutes

The `smoke` and `average-load` profiles are intended for:

- local repeatable verification
- lightweight reliability investigation
- support work for flaky E2E investigation

The `stress` profile is manual-only for now. These assets are not intended for:

- soak testing
- CI release gates

## Prerequisites

1. Start the local stack separately.
2. Ensure the Admin API is reachable.
3. Install `k6` locally or have Docker available.

## Required environment

- `VT_API_BASE_URL`: base URL of the Admin API, for example `http://127.0.0.1:5510`

Optional overrides:

- `VT_K6_PROFILE`: defaults to `smoke`
- `VT_K6_VUS`: override VU count
- `VT_K6_DURATION`: override duration for duration-based profiles
- `VT_K6_RESULTS_DIR`: relative output folder for k6 summary JSON, defaults to `tests/performance/results`
- `VT_K6_USE_DOCKER`: `auto` (default), `0` (force local k6), `1` (force Docker k6)
- `VT_K6_DOCKER_IMAGE`: Docker image used in Docker mode, defaults to `grafana/k6:0.49.0`

## Run with wrapper

```bash
VT_API_BASE_URL=http://127.0.0.1:5510 scripts/run-admin-performance-smoke.sh
```

On Windows PowerShell:

```powershell
$env:VT_API_BASE_URL = 'http://127.0.0.1:5510'
scripts/run-admin-performance-smoke.ps1
```

## Run from Aspire

Start AppHost with `dotnet tool run aspire run`, then start the explicit `admin-performance-smoke`
resource from the Aspire dashboard. AppHost sets `VT_API_BASE_URL` from the Admin API HTTP endpoint
and waits for the API before the smoke resource can run.

Use Aspire execution when you want the run captured alongside local stack logs, resource state, and
dashboard diagnostics. Use the standalone wrapper when reproducing against an already-running API or
when Aspire is not the process owner.

Wrapper behavior:

- uses local `k6` when available
- falls back to Docker when `k6` is missing
- exports summary JSON to the ignored `tests/performance/results/` folder by default
- uses `scripts/run-admin-performance-smoke.ps1` from AppHost on Windows and the Bash wrapper elsewhere
- rewrites `http://127.0.0.1:*`, `http://localhost:*`, `https://127.0.0.1:*`, `https://localhost:*` to `host.docker.internal` in Docker mode
- forwards `VT_K6_VUS` and `VT_K6_DURATION` into Docker mode
- uses `VT_K6_DOCKER_IMAGE` or `grafana/k6:0.49.0` by default in Docker mode

## Run raw k6

```bash
k6 run -e VT_API_BASE_URL=http://127.0.0.1:5510 tests/performance/k6/scenarios/admin-smoke.js
```

## Run raw Docker k6

```bash
docker run --rm \
  --add-host host.docker.internal:host-gateway \
  -v "$(pwd):/work" -w /work \
  grafana/k6:0.49.0 run \
  -e VT_API_BASE_URL=http://host.docker.internal:5510 \
  -e VT_K6_PROFILE=smoke \
  tests/performance/k6/scenarios/admin-smoke.js
```

## Conventions

- keep scenario logic separate from workload/profile configuration
- use local modules for repo-owned helpers
- use checks for assertions and thresholds for pass/fail behavior
- keep tags and groups meaningful, but avoid one group per request
- keep smoke profiles simple and reproducible
