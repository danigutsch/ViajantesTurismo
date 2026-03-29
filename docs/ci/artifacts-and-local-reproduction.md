# Artifacts and local reproduction

This document describes the CI artifacts produced by the main validation workflow and the
local commands used to reproduce common failures.

## Artifacts

When the validation path of the `build-and-test` job runs, the `test-results`,
`coverage-report`, and `sonar-coverage` artifacts are uploaded with `if: always()` so
they are preserved regardless of test success or failure. For documentation-only changes
that take the lightweight docs-only path, these artifacts are not produced or uploaded.
The focused `build-test-diagnostics` artifact is uploaded only when the job fails.

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `test-results` | `**/TestResults/**` from all test projects | 7 days |
| `coverage-report` | Aggregated HTML report under `TestResults/CoverageReport/**` | 7 days |
| `sonar-coverage` | Aggregated SonarCloud coverage input at `TestResults/sonar-coverage.xml` | 7 days |
| `build-test-diagnostics` | Focused summary file under `TestResults/ci-diagnostics/**` when build/test fails | 7 days |

The artifact includes per-project `TestResults` folders, which contain `.trx` result files
and `coverage.cobertura.xml` when coverage collection is enabled.

For local validation, missing result files are treated as an error because that indicates
the test infrastructure did not produce the expected outputs. In CI, artifact upload is
best-effort (`if-no-files-found: ignore`), so missing result files do not by themselves
fail the workflow but should still be investigated.

The HTML coverage artifact is generated from those per-project Cobertura files with the
repo's local `reportgenerator` tool manifest entry.

Coverage now has two consumers inside the same workflow:

- the main CI workflow publishes Cobertura XML inside `test-results` plus an aggregated
  HTML `coverage-report` artifact for direct inspection
- the `Build and Test` job also publishes a `sonar-coverage.xml` input file and sends
  hosted analysis results to SonarCloud

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

When the `Build and Test` job fails before full test artifacts are available, CI also
uploads a small `build-test-diagnostics` artifact containing step outcomes, toolchain
versions, and a `TestResults` inventory snapshot to speed up first-pass diagnosis.

## Reproducing failures locally

All CI commands map directly to local developer commands.

### Build and Test job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet build ViajantesTurismo.slnx --no-restore
bash scripts/install-playwright.sh
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-tests-with-coverage.sh
```

`scripts/run-tests-with-coverage.sh` is a post-build helper. It assumes the
restore/build steps above have already completed and then runs tests, verifies Cobertura
output exists, and generates the aggregated HTML coverage report.

To reproduce the SonarCloud analysis flow locally after configuring the required
environment variables, run:

```bash
export SONAR_TOKEN="..."
export SONAR_ORGANIZATION="..."
export SONAR_PROJECT_KEY="..."
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
bash scripts/install-playwright.sh
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-sonar-analysis.sh
```

For documentation-only changes (`docs/**`, `README.md`, or `CONTRIBUTING.md`), CI skips
the build-and-test commands above but still records a successful `Build and Test` check
through the lightweight docs-only path in the job.

### Lint job

```bash
# From repository root
npm ci --ignore-scripts
npm run lint:all
```

If `npm run lint:all` fails, run individual linters to isolate the failure:

```bash
npm run lint:md        # Markdown
npm run lint:sh        # Shell scripts
npm run lint:json      # JSON files
npm run lint:gherkin   # Gherkin/feature files
```

Auto-fix what can be auto-fixed:

```bash
npm run lint:all:fix
```
