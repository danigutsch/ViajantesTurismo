# Performance Testing

This folder contains repository-owned performance and load testing assets.

## Structure

- `k6/` contains the first implementation based on k6.

Repo terminology stays generic:

- `scenario`: the flow under test
- `profile`: the workload shape applied to the scenario
- `performance testing`: umbrella capability
- `load testing`: one subset of performance testing

## Current scope

The first thin slice is a smoke scenario for Admin API reliability investigation.

Current implementation path:

- `k6/scenarios/admin-smoke.js`

Current workload profiles:

- `smoke`: default local/repro profile
- `average-load`: conservative regular validation profile
- `stress`: manual stress profile for investigation, not CI gating

Current wrapper:

- `../../scripts/run-admin-performance-smoke.sh`
- `../../scripts/run-admin-performance-smoke.ps1`

Current Aspire integration:

- opt-in resource: `admin-performance-smoke`
- enable with `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1`

## Intent

This initial slice is for:

- repeatable local repro support
- lightweight API-level reliability checks
- validating the toolchain and conventions before broader profiles are added

This is not yet a full load-testing suite.

## Run

Start the local stack first, then point the wrapper at the Admin API endpoint:

```bash
VT_API_BASE_URL=http://127.0.0.1:5510 scripts/run-admin-performance-smoke.sh
```

On Windows PowerShell:

```powershell
$env:VT_API_BASE_URL = 'http://127.0.0.1:5510'
scripts/run-admin-performance-smoke.ps1
```

## Run With Aspire

The AppHost can run the smoke scenario after the Admin API starts. This is opt-in so normal Aspire
runs do not execute load tooling accidentally:

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

Use `VT_K6_PROFILE=average-load` for conservative broader validation. Keep `stress` manual-only.

## Results

The wrapper exports k6 summaries to `tests/performance/results/` by default. That folder is ignored
by Git so local investigation output does not become source. Override the relative output folder with
`VT_K6_RESULTS_DIR` when comparing runs or collecting artifacts manually.

Start with `smoke` for local repro. Use `average-load` for broader pre-release validation after the
smoke profile is stable. Keep `stress` manual until thresholds and environment assumptions are proven
stable enough for automation.
