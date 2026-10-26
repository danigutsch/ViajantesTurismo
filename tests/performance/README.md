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
- `k6/scenarios/file-upload-scan.js`

Current workload profiles:

- `smoke`: default local/repro profile
- `average-load`: conservative regular validation profile
- `stress`: manual stress profile for investigation, not CI gating

Current runner:

- `sharedkernel-k6`, configured by `../../.config/sharedkernel-k6.json` with the `file-upload-scan` scenario
- the `admin-smoke` scenario runs only as the AppHost resource below

Current Aspire integration:

- opt-in resource: `admin-performance-smoke`, a k6 container that starts on request
- enable with `VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1`
- AppHost wiring lives in `src/ViajantesTurismo.AppHost/PerformanceTestingResourceExtensions.cs`

## Intent

This initial slice is for:

- repeatable local repro support
- lightweight API-level reliability checks
- validating the toolchain and conventions before broader profiles are added
- real Kestrel request-stream upload scan baselines

This is not yet a full load-testing suite.

## Run

The Admin smoke runs through Aspire (see below). For real multipart upload scanning, run the `file-upload-scan` scenario. `sharedkernel-k6` starts the
isolated benchmark host on an ephemeral loopback Kestrel port, waits for the URL it writes to
`VT_UPLOAD_BENCHMARK_READY_FILE`, probes `/health`, and runs k6 against the real socket:

```bash
dotnet tool run sharedkernel-k6 -- run --scenario file-upload-scan
```

Use `VT_UPLOAD_PAYLOAD_BYTES=<bytes>` to change the deterministic multipart payload size. See
`../../docs/file-and-stream-benchmark-baselines.md` for the design matrix and raw commands.
The tool runs a local `k6` by default. `--container` runs the configured digest-pinned
`grafana/k6` image with Docker instead, mounts the scripts read-only, and never pulls, so pull the image
first; `--image <ref@sha256:digest>` replaces the configured image. The target must be a loopback URL
unless `--allow-external-target` is passed; `--http-debug` needs `--allow-http-debug`, and k6 outputs
such as `--out` need `--allow-remote-output`. k6 arguments follow a second `--`.

The scripts read `VT_K6_PROFILE`, `VT_K6_VUS`, `VT_K6_DURATION`, and, for `file-upload-scan`,
`VT_UPLOAD_PAYLOAD_BYTES` (a whole number from 1 to 16777216) from the environment the tool forwards.
An unknown profile fails the run.

## Run With Aspire

The AppHost can run the smoke scenario against the Admin API. This is opt-in so normal Aspire
runs do not execute load tooling accidentally:

```bash
VT_ASPIRE_ENABLE_PERFORMANCE_TESTS=1 dotnet tool run aspire run
```

Start `admin-performance-smoke` from the Aspire dashboard, or with `dotnet tool run aspire resource
admin-performance-smoke start`, once the Admin API is healthy. Use `VT_K6_PROFILE=average-load` for conservative
broader validation. Keep `stress` manual-only.

The resource is a `grafana/k6` container pinned by tag and digest from
`PandaBear.SharedKernel.Aspire.Hosting.K6`. It receives `VT_API_BASE_URL` from the Admin API endpoint and forwards
`VT_K6_PROFILE` (default `smoke`), `VT_K6_VUS`, and `VT_K6_DURATION` when they are set before AppHost starts. The
scenario signs in with the `k6-smoke` service account, whose `Reader` role holds only the read permissions of the
customer, tour, and booking collection endpoints. The client secret is the generated
`identity-provider-k6-client-secret` parameter; the container receives it as an environment variable, and it never
reaches a file or the k6 output. The container exits when the run ends; a failed token request or a breached threshold
ends it with a non-zero exit code.

## Results

Each `sharedkernel-k6` run writes its k6 summary to its own directory under `tests/performance/results/`, which the
tool prints. The Admin smoke container writes `tests/performance/results/admin-smoke-summary.json`. That folder is
ignored by Git so local investigation output does not become source.

The `file-upload-scan` run directory also holds the benchmark host's `host.stdout.log` and
`host.stderr.log` and the ready file with the discovered ephemeral base URL.

Start with `smoke` for local repro. Use `average-load` for broader pre-release validation after the
smoke profile is stable. Keep `stress` manual until thresholds and environment assumptions are proven
stable enough for automation.
