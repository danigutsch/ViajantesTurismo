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
3. Install `k6` locally.

## Required environment

- `VT_API_BASE_URL`: base URL of the Admin API, for example `http://127.0.0.1:5001`

Optional overrides:

- `VT_K6_PROFILE`: defaults to `smoke`
- `VT_K6_VUS`: override VU count
- `VT_K6_DURATION`: override duration for duration-based profiles

## Run with wrapper

```bash
VT_API_BASE_URL=http://127.0.0.1:5001 scripts/run-admin-performance-smoke.sh
```

## Run raw k6

```bash
k6 run -e VT_API_BASE_URL=http://127.0.0.1:5001 tests/performance/k6/scenarios/admin-smoke.js
```

## Conventions

- keep scenario logic separate from workload/profile configuration
- use local modules for repo-owned helpers
- use checks for assertions and thresholds for pass/fail behavior
- keep tags and groups meaningful, but avoid one group per request
- keep smoke profiles simple and reproducible
