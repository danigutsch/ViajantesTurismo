# k6 Performance Testing

This folder contains the first performance/load testing implementation for this repository.

## Scenario

- `scenarios/admin-smoke.js`: small Admin API smoke/repro scenario

## Supported profile

The current scenario uses a single conservative smoke profile.

It is intended for:

- local repeatable verification
- lightweight reliability investigation
- support work for flaky E2E investigation

It is not intended for:

- large stress testing
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
- `VT_K6_USE_DOCKER`: `auto` (default), `0` (force local k6), `1` (force Docker k6)

## Run with wrapper

```bash
VT_API_BASE_URL=http://127.0.0.1:5510 scripts/run-admin-performance-smoke.sh
```

Wrapper behavior:

- uses local `k6` when available
- falls back to Docker when `k6` is missing
- rewrites `http://127.0.0.1:*`, `http://localhost:*`, `https://127.0.0.1:*`, `https://localhost:*` to `host.docker.internal` in Docker mode
- forwards `VT_K6_VUS` and `VT_K6_DURATION` into Docker mode

## Run raw k6

```bash
k6 run -e VT_API_BASE_URL=http://127.0.0.1:5510 tests/performance/k6/scenarios/admin-smoke.js
```

## Run raw Docker k6

```bash
docker run --rm \
  --add-host host.docker.internal:host-gateway \
  -v "$(pwd):/work" -w /work \
  grafana/k6:latest run \
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
