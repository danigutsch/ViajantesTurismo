# CI Governance and Rollout

Operational details, artifact guidance, failure reproduction steps, and action trust policy for
the GitHub Actions CI workflow (`.github/workflows/ci.yml`).

## Workflow Jobs

The CI workflow runs on every pull request targeting `main`, every push to `main`, and on
`workflow_dispatch`. It defines two parallel jobs:

### Build and Test

| Attribute | Value |
| --- | --- |
| Job key | `build-and-test` |
| Job name | `Build and Test` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Detect whether the change set is documentation-only using `git diff`
3. Set up .NET SDK from `global.json` (`actions/setup-dotnet`) when build/test work is required
4. Set up Node.js from `.nvmrc` (`actions/setup-node`) when build/test work is required
5. `dotnet restore ViajantesTurismo.slnx` when build/test work is required
6. `dotnet tool restore` when build/test work is required
7. `dotnet build ViajantesTurismo.slnx --no-restore` when build/test work is required
8. Install Playwright browsers via `bash scripts/install-playwright.sh` when build/test work is required; on supported Ubuntu CI images it also installs
 OS dependencies
9. Trust HTTPS developer certificate and set `SSL_CERT_DIR` when build/test work is required
10. Run `bash scripts/run-tests-with-coverage.sh` when build/test work is required;
  this helper runs the coverage-enabled test command, verifies Cobertura output exists,
  and generates the HTML coverage report
11. When build or test work fails, create a focused diagnostic summary under `TestResults/ci-diagnostics/`
12. Upload test result artifacts and coverage report artifacts (`actions/upload-artifact`, runs on `always()` after the test step executes) when tests ran
13. Upload the focused `build-test-diagnostics` artifact when the job fails

For pull requests and pushes that only modify `docs/**`, `README.md`, or `CONTRIBUTING.md`,
the job still runs and reports a successful `Build and Test` check, but it skips the expensive
restore, build, Playwright, and test steps. This avoids the "Pending required check" problem
caused by trigger-level `paths` or `paths-ignore` filters.

The change classification logic is implemented in `scripts/detect-changes.sh`, not inline in the
workflow YAML. If the script cannot determine the diff range reliably, it fails open by setting
`build_required=true` so CI prefers extra work over a false skip.

> **Note:** Step 8 works around a [known SDK bug](https://github.com/dotnet/aspnetcore/issues/65391)
> where `dotnet dev-certs https --trust` exits with code 4 on `ubuntu-latest` in SDK 10.0.103+.
> The step uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP clients in
> the test run trust the per-user dev certificate.

### Lint

| Attribute | Value |
| --- | --- |
| Job key | `lint` |
| Job name | `Lint` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Set up Node.js from `.nvmrc` with npm cache (`actions/setup-node`)
3. `npm ci --ignore-scripts`
4. `npm run lint:all`

## SonarCloud Workflow

Hosted code quality analysis is implemented in a separate workflow:
`.github/workflows/sonar.yml`.

This mirrors the specialized-workflow pattern used by BookWorm, where the main CI flow remains
focused on build, test, and lint concerns while SonarCloud analysis runs in its own pipeline.

### SonarCloud

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/sonar.yml` |
| Primary job name | `SonarCloud` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository with full history (`actions/checkout` with `fetch-depth: 0`)
2. Detect whether the change set is documentation-only using `git diff`
3. Run the shared CI setup action (`.github/actions/setup-ci-prerequisites`) when analysis work is required;
  this provisions the pinned .NET and Node toolchains, restores the solution, restores local
  tools, and trusts the HTTPS developer certificate
4. Cache SonarCloud packages under `~/.sonar/cache`
5. Run `bash scripts/run-sonar-analysis.sh` when analysis work is required
6. Upload the Sonar coverage input artifact (`TestResults/sonar-coverage.xml`) for troubleshooting

The workflow uses the same docs-only detection strategy as the main CI workflow so that future
required checks can still resolve cleanly without trigger-level `paths` filters.

`scripts/run-sonar-analysis.sh` wraps the `begin` / `build` / `coverage collection` /
`coverage conversion` / `end` pattern for `.NET` projects. The current hosted path reuses the
repo's canonical coverage collection helper (`scripts/collect-test-coverage.sh`) to generate
per-project Cobertura files, then converts that coverage into SonarQube generic format with the
repo-pinned `reportgenerator` tool before publishing `TestResults/sonar-coverage.xml` to the
scanner.

SonarCloud `Automatic Analysis` must stay disabled for this project. The repository already
runs hosted analysis through `.github/workflows/sonar.yml`, and enabling both modes causes the
workflow job to fail with a duplicate-analysis error.

## Artifacts

Test result artifacts are uploaded by the `build-and-test` job unconditionally (`if: always()`).

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `test-results` | `**/TestResults/**` from all test projects | 7 days |
| `coverage-report` | Aggregated HTML report under `TestResults/CoverageReport/**` | 7 days |
| `build-test-diagnostics` | Focused summary file under `TestResults/ci-diagnostics/**` when build/test fails | 7 days |

The artifact includes per-project `TestResults` folders, which contain `.trx` result files and
`coverage.cobertura.xml` when coverage collection is enabled.
If the test step ran, missing result files are treated as an error because that indicates the
test infrastructure did not produce the expected outputs.
The HTML coverage artifact is generated from those per-project Cobertura files with the repo's
local `reportgenerator` tool manifest entry.

Coverage now has two consumers:

- The main CI workflow publishes Cobertura XML inside `test-results` plus an aggregated HTML
  `coverage-report` artifact for direct inspection.
- The SonarCloud workflow publishes a separate `sonar-coverage.xml` input file and sends hosted
  analysis results to SonarCloud.

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

When the `Build and Test` job fails before full test artifacts are available, CI also uploads a
small `build-test-diagnostics` artifact containing step outcomes, toolchain versions, and a
`TestResults` inventory snapshot to speed up first-pass diagnosis.

## Reproducing Failures Locally

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

`scripts/run-tests-with-coverage.sh` is a post-build helper. It assumes the restore/build steps
above have already completed and then runs tests, verifies Cobertura output exists, and generates
the aggregated HTML coverage report.

To reproduce the SonarCloud analysis flow locally after configuring the required environment
variables, run:

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

For documentation-only changes (`docs/**`, `README.md`, or `CONTRIBUTING.md`), CI skips the
build-and-test commands above but still records a successful `Build and Test` check.

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

## Required Status Checks

Once branch protection is configured for `main`, require these exact job names:

- `Build and Test`
- `Lint`
- `Dependency Review`
- `SonarCloud`

These names match the `name:` fields in `.github/workflows/ci.yml` and
`.github/workflows/dependency-review.yml`. The `SonarCloud` check comes from
`.github/workflows/sonar.yml`. Any rename of the jobs must be reflected in branch protection
settings.

## Action Versioning Policy

All external GitHub Actions used in repository workflows are pinned to **immutable commit SHAs**
with the upstream release noted in an inline comment (for example
`actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6`).

### Rationale

Immutable SHA pinning is the repository baseline for workflow supply-chain hardening:

- GitHub executes the exact reviewed revision referenced by the workflow, not whatever a tag
  is moved to later.
- Inline version comments preserve readability and make review of Dependabot action updates
  straightforward.
- The workflow surface still stays narrow because the repository uses only official
  GitHub-maintained actions.

### Trusted actions

The workflow uses only official GitHub-maintained actions:

| Action | Purpose |
| --- | --- |
| `actions/checkout` | Repository checkout |
| `actions/setup-dotnet` | .NET SDK provisioning |
| `actions/setup-node` | Node.js provisioning |
| `actions/cache` | SonarCloud package cache |
| `actions/upload-artifact` | Test result artifact upload |
| `actions/dependency-review-action` | PR dependency and license review |

The GitHub workflow surface still uses only official GitHub-maintained actions.
SonarCloud integration is implemented through repo-pinned local `.NET` tools rather than an
additional third-party GitHub Action. The current hosted analysis path actively uses:

- `dotnet-sonarscanner` `11.2.0`
- `dotnet-reportgenerator-globaltool` `5.5.1`

The local tool manifest also contains `dotnet-coverage` `18.5.2` for supported `.NET` coverage
workflows, but the current SonarCloud workflow does not rely on the direct `dotnet-coverage -f xml`
path.

This keeps the GitHub Actions dependency surface narrow while still adopting the SonarCloud
analysis model used in BookWorm's quality strategy.

The SonarCloud workflow requires these GitHub repository settings:

- Actions secret `SONAR_TOKEN`
- Repository variable `SONAR_ORGANIZATION`
- Repository variable `SONAR_PROJECT_KEY`
- SonarCloud project setting: `Automatic Analysis` disabled

### Update process

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The configuration
  covers `github-actions`, `nuget`, and `npm` ecosystems on a weekly schedule.
- When Dependabot proposes an action update, review both the release notes and the resolved SHA,
  then verify the affected workflows still pass before merging.
- When upgrading across major action versions, review the migration guidance before accepting the
  new SHA-pinned reference.

## Workflow Ownership (CODEOWNERS)

The `CODEOWNERS` file at the repository root requires review for all changes to workflow files.

Any pull request that modifies `.github/workflows/**` will request review from the designated
code owners. See `CODEOWNERS` for the current ownership mapping.

## Dependency Review Workflow

A separate workflow (`.github/workflows/dependency-review.yml`) runs the
`actions/dependency-review-action` on every pull request. It scans manifest and lock file
changes for newly introduced vulnerabilities and fails the check when severity is `moderate`
or higher.

This workflow is intentionally separate from the main CI workflow so that its required check
status does not interfere with path-based optimizations in the CI workflow.

## Devcontainer Smoke Workflow

A separate workflow (`.github/workflows/devcontainer-smoke.yml`) runs supplemental devcontainer
validation on a weekly schedule, on manual dispatch, and for pull requests or pushes that touch
devcontainer/bootstrap inputs such as `.devcontainer/**`, `.nvmrc`, `global.json`, or dependency
manifests for npm and NuGet packages.

### Devcontainer Smoke

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/devcontainer-smoke.yml` |
| Primary job name | `Devcontainer Smoke` |
| Runner | `ubuntu-latest` |
| Merge gate | Not required |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Set up Node.js from `.nvmrc` (`actions/setup-node`)
3. Run `devcontainer up` via the pinned `@devcontainers/cli` package to build and start the
  repository devcontainer
4. Allow the configured `onCreateCommand`, `postCreateCommand`, and `postStartCommand` lifecycle
  hooks to execute as part of container creation
5. Run `devcontainer exec` to verify `.NET`, Node.js, Git, and Docker access inside the container
6. Parse the started container ID from the `devcontainer up` log and remove the container with
  `docker rm -f` during cleanup
7. Upload `devcontainer-smoke-logs` when the workflow fails

This workflow is intentionally supplemental rather than required. It is meant to catch
environment drift in the repository's containerized developer path without expanding the required
pull-request gate for ordinary application changes.

## Dependabot Configuration

`.github/dependabot.yml` automates version update PRs for three ecosystems:

| Ecosystem | Scope | Schedule |
| --- | --- | --- |
| `github-actions` | Workflow action references | Weekly |
| `nuget` | .NET package dependencies | Weekly |
| `npm` | Node.js dependencies | Weekly |

Dependabot PRs use conventional commit prefixes (`ci` for actions, `deps` for packages) and
include scope annotations.

## Branch Protection Rules

Branch protection for `main` is configured to require the following status checks:

- `Build and Test` (from `.github/workflows/ci.yml`)
- `Lint` (from `.github/workflows/ci.yml`)
- `Dependency Review` (from `.github/workflows/dependency-review.yml`)
- `SonarCloud` (from `.github/workflows/sonar.yml`)

These names match the `name:` fields in the respective workflow files. Any rename of the jobs
must be reflected in branch protection settings.

Representative pull request validation has also been observed successfully with these checks,
including the main CI workflow, the separate dependency review workflow, and the SonarCloud
workflow. The `Devcontainer Smoke` workflow remains supplemental and is not part of the required
merge gate.

## Next Required Work

The near-term required items from the initial rollout are complete. Monitor the workflow in
normal use and move deferred items into scope only when there is a concrete operational need.

## Planned Follow-Up Work

The following follow-up items are planned after the baseline rollout:

- Multi-OS matrix (not required until a concrete cross-platform requirement appears)
- Coverage thresholds (planned for enforcement after baseline coverage trends and threshold policy are stable)

## Related Documentation

- [README — Continuous Integration](../README.md#continuous-integration) - Contributor-facing CI summary
- [Code Quality Tools](CODE_QUALITY.md) - Local linting and formatting tools
- [PBI-2026-03-15-02](backlog/PBI-2026-03-15-02-github-actions-ci-workflow.md) - Original delivery plan
