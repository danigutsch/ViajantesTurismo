# Artifacts and local reproduction

This document describes the CI artifacts produced by the main validation workflow and the
local commands used to reproduce common failures.

## Artifacts

When CI executes a test slice, that job uploads its own `*-results` artifact with
`if: always()` so test outputs and coverage XML survive both passing and failing runs.
For documentation-only changes, the lightweight skip path runs instead and no slice
artifacts are produced. Each slice also uploads a focused `*-diagnostics` artifact only
when that job fails.

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `fast-validation-results` | `**/TestResults/**` produced by the fast test slice | 7 days |
| `admin-integration-test-results` | `**/TestResults/**` produced by the provider/database integration slice | 7 days |
| `admin-api-integration-test-results` | `**/TestResults/**` produced by the dedicated Admin API integration slice | 7 days |
| `admin-system-test-results` | `**/TestResults/**` produced by the admin system slice | 7 days |
| `mediator-heavy-test-results` | `**/TestResults/**` produced by the mediator-heavy slice | 7 days |
| `sonar-coverage` | Aggregated SonarCloud coverage input at `TestResults/sonar-coverage.xml` | 7 days |
| `sonar-analysis-log` | Raw SonarCloud analysis log from the dedicated Sonar job | 7 days |
| `sonar-analysis-manifest` | Machine-readable Sonar job manifest at `TestResults/ci-validation-manifest.json` | 7 days |
| `coverage-report` | Aggregated HTML coverage report under `TestResults/CoverageReport/**` from the Sonar aggregation job | 7 days |
| `fast-validation-diagnostics` | Focused failure summary for the fast validation slice | 7 days |
| `admin-integration-test-diagnostics` | Focused failure summary for the provider/database integration slice | 7 days |
| `admin-api-integration-test-diagnostics` | Focused failure summary for the dedicated Admin API integration slice | 7 days |
| `admin-system-test-diagnostics` | Focused failure summary for the admin system slice | 7 days |
| `mediator-heavy-test-diagnostics` | Focused failure summary for the mediator-heavy slice | 7 days |

The slice result artifacts also include machine-readable helper outputs such as
`*-phase-timings.tsv` and `*-manifest.json`, alongside the per-project `TestResults`
folders that contain `.trx` result files and `coverage.cobertura.xml` when coverage
collection is enabled.

For local validation, missing result files are treated as an error because that indicates
the test infrastructure did not produce the expected outputs. In CI, artifact upload is
best-effort (`if-no-files-found: ignore`), so missing result files do not by themselves
fail the workflow but should still be investigated.

The HTML coverage artifact is generated from those per-project Cobertura files with the
repo's local `reportgenerator` tool manifest entry.

Coverage now has two consumers inside the same workflow:

- the test-slice jobs publish Cobertura XML inside their `*-results` artifacts
- the dedicated `SonarCloud` job downloads those slice artifacts, generates the aggregated
  HTML `coverage-report`, creates `sonar-coverage.xml`, and sends the hosted analysis to
  SonarCloud

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

When a validation slice fails before full test artifacts are available, CI also uploads a
small `*-diagnostics` artifact containing step outcomes, toolchain versions, a
`TestResults` inventory snapshot, the captured phase timing tables, and any generated
manifest files to speed up first-pass diagnosis.

## Reproducing failures locally

All CI commands map directly to local developer commands.

### Fast Validation job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Fast Validation" \
  --projects-file scripts/ci-test-slices/fast-validation.txt
```

`scripts/run-ci-test-slice.sh` is a post-restore helper. It builds single-project slices directly,
builds multi-project slices through one temporary solution, runs them with coverage, and records
per-slice timing information. Aggregated HTML coverage is generated once later by the `SonarCloud`
job.

### Admin Integration Tests job

This residual slice exercises provider/database integration projects. The dedicated Admin API
integration project runs in the next lane.

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Admin Integration Tests" \
  --projects-file scripts/ci-test-slices/admin-integration.txt
```

### Admin API Integration Tests job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Admin API Integration Tests" \
  --projects-file scripts/ci-test-slices/admin-api-integration.txt
```

### Admin System Tests job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Admin System Tests" \
  --install-playwright \
  --projects-file scripts/ci-test-slices/admin-system.txt
```

### Mediator Heavy Tests job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
bash scripts/run-ci-test-slice.sh \
  --slice-name "Mediator Heavy Tests" \
  --projects-file scripts/ci-test-slices/mediator-heavy.txt
```

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

To reproduce the Sonar aggregation path after the test slices have already produced
coverage files, run:

```bash
export SONAR_TOKEN="..."
export SONAR_ORGANIZATION="..."
export SONAR_PROJECT_KEY="..."
dotnet restore ViajantesTurismo.slnx
dotnet tool restore
bash scripts/generate-sonar-coverage-report.sh
SONAR_ANALYSIS_SKIP_TESTS=true bash scripts/run-sonar-analysis.sh
```

For documentation-only or low-risk contributor-maintenance changes (`docs/**`, `README.md`,
`CONTRIBUTING.md`, and the allowlisted scripts in `scripts/detect-changes.sh`), CI skips the
validation commands above. The affected test jobs use lightweight skip paths, and the required
`Build and Test` aggregate records their successful outcomes.

### Lint job

```bash
# From repository root
bash scripts/lint-all.sh
```

If the CI lint job fails, run individual linters to isolate the failure:

```bash
bash scripts/lint-markdown.sh              # Markdown
shellcheck **/*.sh                         # Shell scripts
bash scripts/lint-json.sh **/*.json        # JSON files
bash scripts/lint-gherkin.sh tests/**/*.feature  # Gherkin/feature files
```
