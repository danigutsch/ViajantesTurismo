# Integration and system test diagnostics proposal

Status: researched design; targeted resource/trace capture is not implemented by this document.
Owner: application test and CI maintainers. Reviewed October 20, 2026.
Scope: ViajantesTurismo suites; no SharedKernel API change or new diagnostic dependency is required
by this proposal. Ordinary successful runs should remain quiet.

## Contents

- [Current evidence](#current-evidence)
- [Minimum failure bundle](#minimum-failure-bundle)
- [Correlation and privacy](#correlation-and-privacy)
- [Local and CI retrieval](#local-and-ci-retrieval)
- [Implementation sequence](#implementation-sequence)
- [Official research](#official-research)

## Current evidence

The [main workflow](../../.github/workflows/ci.yml) already uploads test results and run evidence per
slice with seven-day retention. `sharedkernel-ci run` writes `phase-timings.tsv`, `manifest.json`,
`diagnostics.txt` (phase status, escaped failure message, and file inventory), and one log per phase to
`TestResults/selection/execution-<slice>/`, whether the run passes, fails, or is canceled, and a failed
slice prints `dotnet --info`.
These are current capabilities, not proof of test-to-resource trace correlation.

The [hosted-test assembly fixture](../../tests/ViajantesTurismo.HostedTests/Infrastructure/HostedTestFixture.cs) starts each
container on first demand. The
[System class fixture](../../tests/ViajantesTurismo.HostedTests/Admin/System/Infrastructure/Fixtures/AspireSystemTestFixture.cs)
borrows the `system` stack that the assembly fixture starts on demand, authenticates resource clients, and warms
the web surfaces. Its
[Playwright base](../../tests/ViajantesTurismo.HostedTests/Admin/System/Infrastructure/Bases/AspireSystemTestBase.cs)
provides pages and navigation. The inspected fixture/base do not implement the per-test bounded
resource-log/OTel bundle below. Do not claim an artifact exists until its producer is implemented.

## Minimum failure bundle

Use a generated opaque attempt ID, never a customer name, route value, or test display name
containing theory input, in artifact paths.

| Artifact | Proposed contents | Limit and default |
| --- | --- | --- |
| Manifest JSON | Commit, suite/class/method identifiers, opaque attempt/AppHost IDs, UTC start/end, result, resource names/states, available trace IDs, collection outcomes | 64 KiB; no raw arguments, environment dump, tokens, or connection strings |
| Test result/output | Existing test result plus bounded sanitized stage/outcome messages | 256 KiB per failed attempt; retain original test failure if capture fails |
| Resource log excerpt | Relevant resource and failure time window; allowlisted event IDs, severity, timestamp, sanitized message template and safe properties | 200 events and 256 KiB per resource, maximum four resources; no broad debug logging |
| Trace summary | Trace/span/parent IDs, safe operation name, resource, timing, status; identify export/sampling absence | 500 spans and 1 MiB per attempt; no request/response bodies or baggage export |
| Browser evidence | Failure step and sanitized navigation category; screenshot/trace only for reviewed synthetic-data scenarios | Disabled by default; optional trace up to 10 MiB and one screenshot up to 2 MiB, both inside the total budget |

Proposed total bundle budget: 16 MiB per attempt, 10 failed attempts per slice, then a truncation
manifest. Collect at most the final 60 seconds before failure plus 5 seconds after. Limit collector
work to 10 seconds and cancel it independently from the failed test's canceled token. Mark timeouts,
truncation, missing resources, and sampling gaps explicitly. These limits are design choices for
implementation review, not current runner settings.

## Correlation and privacy

Create a test-attempt Activity and record its TraceId alongside the opaque attempt ID. Test-owned
clients propagate standard trace context through requests; record actual downstream trace IDs
rather than assuming browser navigation inherits a .NET Activity. Async transport correlation must
use existing envelope/Activity semantics or explicitly report a missing link. Do not add test names,
PII, or high-cardinality IDs to metric dimensions.

Give each AppHost fixture lifetime an opaque ID and associate resource logs with that ID and test
time window. Parallel tests must keep their own attempt IDs; time-window overlap alone does not
prove which test caused a log. Preserve xUnit/MTP parallelism and assembly-owned fixture lifetimes;
do not introduce global locks, shared fixture processes, or a "current test" singleton.

Prefer allowlisting safe structured fields before persistence. Collector redaction is defense in
depth, not permission to record secrets initially. Exclude auth/cookie/session state, headers,
URLs with query/identity values, SQL parameters, request/response bodies, imported files, document
content, screenshots of personal data, and full exception text that may embed payloads. Hashing a
sensitive value does not automatically anonymize it.

Playwright traces can contain DOM/network data; failure-only retention does not make them safe.
Enable only in a synthetic-data scenario with reviewed capture options and artifact inspection.
Crash/hang dumps can contain process memory and require a separate explicit opt-in/private handling
path; do not upload them by default or install dump extensions for ordinary failures.

## Local and CI retrieval

Today: reproduce with the existing focused MTP invocation, then inspect the suite's result directory
and slice phase log. Choose a known failing method without sensitive theory arguments:

```bash
dotnet test --project tests/ViajantesTurismo.HostedTests/ViajantesTurismo.HostedTests.csproj \
    --filter-method '<fully-qualified-method>'
```

Replace the placeholder with the actual method. Use pinned SDK/tools and the suite's existing
startup prerequisites. For a manually launched AppHost, the dashboard's resource state, structured
logs, console logs, then traces are the diagnostic order. A manual host is not automatically the
fixture's host, and dashboard data may not survive fixture disposal.

In CI, open the failing slice's summary, download `ci-test-slice-<slice>-results`, inspect
`diagnostics.txt` and the phase logs first, then locate the affected test. The
proposed per-attempt manifests would link test failures to bounded excerpts; they are not yet
present. Keep the existing seven-day retention for the proposed bundle and use repository access
controls; do not attach raw artifacts to public issues or external viewers.

For an approved local Playwright trace, use the already generated `playwright.ps1 show-trace`
entrypoint in that test project's build output. Do not use transient npm installation or upload
traces to a service to inspect them. Retrieve OTel spans only from the test-owned exporter/backend;
production telemetry access is outside this test workflow.

## Implementation sequence

1. Add an app-owned manifest and attempt correlation in one failing synthetic System scenario.
   Verify parallel isolation, cancellation, failure preservation, and sanitized output.
2. Add bounded resource-event and trace summaries using supported diagnostics APIs for the pinned
   Aspire version; verify behavior against that version before extending the fixture. Avoid changes
   to reusable packages unless a separately reviewed consumer requirement proves necessity.
3. Connect the bounded output to existing CI artifacts, using an allowlist instead of a broader
   archive glob. Add deterministic tests for limits, truncation, unsafe-field omission, and capture
   failure. Keep original exit status and retain setup/teardown failures.
4. Add optional synthetic browser capture only after privacy inspection. Measure diagnostic overhead
   and signal usefulness before extending to other suites; do not enable global debug/live output.

## Official research

Reviewed October 20, 2026. Upstream describes capabilities; the limits and rollout above are local
recommendations, not upstream defaults.

- [Aspire testing and log capture](https://aspire.dev/testing/write-your-first-test/): hosting tests and forwarding logs require deliberate setup.
- [xUnit output capture](https://xunit.net/docs/capturing-output.html): test output and diagnostic messages differ; v3 console capture is optional.
- [MTP crash/hang diagnostics](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-crash-hang-dumps):
  dump extensions are separate packages, not assumed installed here.
- [Playwright .NET Trace Viewer](https://playwright.dev/dotnet/docs/trace-viewer): traces support local inspection and can include detailed browser evidence.
- [OpenTelemetry sensitive-data handling](https://opentelemetry.io/docs/security/handling-sensitive-data/):
  minimize data collection and protect telemetry access.
