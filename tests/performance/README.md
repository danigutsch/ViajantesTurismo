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

Current Aspire resource:

- `admin-performance-smoke`, registered in `ViajantesTurismo.AppHost` as an explicit-start executable

## Intent

This initial slice is for:

- repeatable local repro support
- lightweight API-level reliability checks
- validating the toolchain and conventions before broader profiles are added

This is not yet a full load-testing suite.

## Run Modes

### Standalone

Start the local stack first, then point the wrapper at the Admin API endpoint:

```bash
VT_API_BASE_URL=http://127.0.0.1:5510 scripts/run-admin-performance-smoke.sh
```

On Windows PowerShell:

```powershell
$env:VT_API_BASE_URL = 'http://127.0.0.1:5510'
scripts/run-admin-performance-smoke.ps1
```

### Aspire Dashboard

Start the AppHost:

```bash
dotnet tool run aspire run
```

The AppHost registers `admin-performance-smoke` as an explicit-start executable. Start it from the
Aspire dashboard after the Admin API is healthy. The resource uses service discovery to set
`VT_API_BASE_URL` and waits for the API resource before running.

Keep this resource explicit-start only. Performance smoke runs are investigation and pre-release
tools, not a default part of every local application startup.

## Results

The wrapper exports k6 summaries to `tests/performance/results/` by default. That folder is ignored
by Git so local investigation output does not become source. Override the relative output folder with
`VT_K6_RESULTS_DIR` when comparing runs or collecting artifacts manually.

Start with `smoke` for local repro. Use `average-load` for broader pre-release validation after the
smoke profile is stable. Keep `stress` manual until thresholds and environment assumptions are proven
stable enough for automation.
