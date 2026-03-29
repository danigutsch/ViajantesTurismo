# CI Governance and Rollout

Operational details, artifact guidance, failure reproduction steps, and action trust policy for
the GitHub Actions CI workflow (`.github/workflows/ci.yml`).

## Workflow Jobs

The CI workflow runs on every pull request targeting `main`, every push to `main`, on merge queue
checks (`merge_group`), and on `workflow_dispatch`. It defines three primary jobs plus the shared
change-detection gate:

The workflow-level concurrency policy cancels stale runs for non-`main` refs but preserves in-flight
`main` runs. This keeps pull request iteration responsive while ensuring the protected branch keeps
its post-merge validation history intact.

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
7. Cache SonarCloud packages under `~/.sonar/cache` when validation work is required
8. Run `bash scripts/run-sonar-analysis.sh` when validation work is required; this script wraps
  the SonarScanner for .NET `begin` / `build` / `coverage collection` / `coverage conversion` /
  `end` flow and produces both HTML coverage output and the SonarQube XML coverage input
9. When validation work fails, create a focused diagnostic summary under `TestResults/ci-diagnostics/`
10. Upload test result artifacts, HTML coverage artifacts, and the Sonar coverage input artifact
   (`actions/upload-artifact`, runs on `always()` after the validation step executes) when
   validation ran
11. Upload the focused `build-test-diagnostics` artifact when the job fails

The `test-results`, `coverage-report`, and `sonar-coverage` uploads are intentionally best-effort.
If validation fails before those files exist, CI should report the actual build/test/Sonar failure
instead of adding secondary "artifact missing" noise. The focused diagnostics artifact remains
strict because it is part of the failure-investigation path.

For pull requests and pushes that only modify `docs/**`, `README.md`, or `CONTRIBUTING.md`,
the job still runs and reports a successful `Build and Test` check, but it skips the expensive
restore, build, Playwright, and test steps. This avoids the "Pending required check" problem
caused by trigger-level `paths` or `paths-ignore` filters.

The change classification logic is implemented in `scripts/detect-changes.sh`, not inline in the
workflow YAML. If the script cannot determine the diff range reliably, it fails open by setting
`build_required=true` so CI prefers extra work over a false skip.

> **Note:** The CI setup path works around a [known SDK bug](https://github.com/dotnet/aspnetcore/issues/65391)
> where `dotnet dev-certs https --trust` exits with code 4 on `ubuntu-latest` in SDK 10.0.103+.
> The setup action uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP clients in
> the test run trust the per-user dev certificate.

### SonarCloud

| Attribute | Value |
| --- | --- |
| Job key | `sonarcloud` |
| Job name | `SonarCloud` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Wait for `build-and-test`
2. Report a distinct `SonarCloud` required check result without rerunning build, test, or analysis

This job is intentionally lightweight. It exists so branch protection can keep a dedicated
`SonarCloud` check name while the actual Sonar analysis runs only once inside the `Build and Test`
job.

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

## SonarCloud Analysis Path

Hosted code quality analysis is executed from the main CI workflow rather than from a second
workflow. The repository keeps Sonar operational configuration in version-controlled workflow and
script files so that analysis behavior remains reproducible without depending on SonarCloud UI
features that may not be available on the current plan.

`scripts/run-sonar-analysis.sh` wraps the `begin` / `build` / `coverage collection` /
`coverage conversion` / `end` pattern for `.NET` projects. The hosted path reuses the repo's
canonical coverage collection helper (`scripts/collect-test-coverage.sh`) to generate per-project
Cobertura files, then converts that coverage into both HTML and SonarQube XML formats with the
repo-pinned `reportgenerator` tool before publishing `TestResults/sonar-coverage.xml` to the
scanner.

SonarCloud `Automatic Analysis` must stay disabled for this project. The repository already runs
hosted analysis through GitHub Actions, and enabling both modes causes duplicate-analysis errors.

The repository already relies on an 80% coverage threshold configured in SonarCloud. Coverage
threshold enforcement is therefore part of the hosted Sonar quality gate rather than a missing
future CI feature in this repository.

## Recommended Workflow Evolution

The previous split between `.github/workflows/ci.yml` and `.github/workflows/sonar.yml` duplicated
expensive setup, build, Playwright installation, test, and coverage work on the same pull
requests and on the subsequent merge commit to `main`.

After reviewing current GitHub Actions and SonarQube Cloud guidance, the repository now uses the
recommended consolidated model: SonarCloud analysis runs inside the main validation workflow
instead of trying to reuse build artifacts across separate workflows.

### Recommendation summary

- Keep validation on both pull requests targeting `main` and pushes to `main`.
- Move SonarScanner for .NET execution into the same workflow that performs build, test, and
  coverage collection.
- Keep `lint` as an independent job if separate status visibility remains useful.
- Add the `merge_group` trigger if merge queue is enabled for the repository.
- Keep essential SonarCloud configuration in-repo when UI-level administration is limited on the
  current plan, and prefer UI settings only when they are actually available and sustainable.

### Why this is the recommended direction

For .NET repositories, SonarScanner is designed around a `begin` → `build` → `test/coverage` →
`end` flow. Reusing artifacts from a separate CI workflow would reduce YAML duplication at best,
but it would not remove the requirement to run a Sonar-wrapped build. Consolidating the build,
test, coverage, and Sonar steps into one validation workflow removes the duplicated runner work
while keeping the analysis model aligned with Sonar's recommended usage.

The second run after merge is still expected and desirable. GitHub Actions treats `pull_request`
and `push` as separate events, so a validation run on the PR and a follow-up validation run on
the merge commit to `main` are normal. The post-merge run confirms the actual branch state rather
than only the pre-merge PR state.

### Operational guidance

- Reusable workflows are appropriate for reducing YAML duplication, but they do not eliminate the
  compute cost of repeated build/test execution.
- `workflow_run` chaining is not the preferred solution for this repository's SonarCloud path.
  It adds complexity and security considerations without solving the SonarScanner for .NET build
  coupling.
- `pull_request_target` should not be used for build, test, or Sonar analysis of untrusted pull
  request code.
- Existing workflow-level `concurrency` remains appropriate for canceling stale runs on the same
  ref, but `main` should be exempt from cancellation so post-merge validation is not interrupted.

### Recommended target state

The intended end state is a single primary validation workflow that:

1. Detects docs-only changes.
2. Runs build, Playwright setup, tests, coverage, and Sonar analysis in one build pipeline when
   validation work is required.
3. Continues to run on pull requests to `main`, pushes to `main`, and manual dispatch.
4. Adds `merge_group` coverage if merge queue is adopted.
5. Preserves required check names expected by branch protection.

### Analysis Exclusions

The scanner begin command in `scripts/run-sonar-analysis.sh` sets `sonar.exclusions` to skip
auto-generated code that should not be analyzed:

| Pattern | Reason |
| --- | --- |
| `**/Migrations/**` | EF Core migration files are scaffolded by `dotnet ef` and should not be manually edited |

To add further exclusions, append additional comma-separated glob patterns to the existing
`sonar.exclusions` property in the script.

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

Coverage now has two consumers inside the same workflow:

- The main CI workflow publishes Cobertura XML inside `test-results` plus an aggregated HTML
  `coverage-report` artifact for direct inspection.
- The `Build and Test` job also publishes a `sonar-coverage.xml` input file and sends hosted
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
- `Secret Scan`
- `SonarCloud`

These names match the `name:` fields in `.github/workflows/ci.yml`,
`.github/workflows/dependency-review.yml`, and `.github/workflows/secret-scan.yml`. The
`SonarCloud` check is now emitted by a lightweight job in `.github/workflows/ci.yml` while the
actual analysis executes inside `Build and Test`. Any rename of the jobs must be reflected in
branch protection settings.

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
| `github/codeql-action/upload-sarif` | Upload SARIF results from secret scanning |

SonarCloud integration is implemented through repo-pinned local `.NET` tools rather than an
additional third-party GitHub Action. The workflow lint path also installs the pinned
`actionlint` `1.7.9` release binary directly rather than adding another GitHub Action dependency.
The current hosted analysis path actively uses:

- `dotnet-sonarscanner` `11.2.0`
- `dotnet-reportgenerator-globaltool` `5.5.1`

The local tool manifest also contains `dotnet-coverage` `18.5.2` for supported `.NET` coverage
workflows, but the current SonarCloud workflow does not rely on the direct `dotnet-coverage -f xml`
path.

This keeps the GitHub Actions dependency surface narrow while still adopting the SonarCloud
analysis model used in BookWorm's quality strategy.

The integrated SonarCloud analysis path requires these GitHub repository settings:

- Actions secret `SONAR_TOKEN`
- Repository variable `SONAR_ORGANIZATION`
- Repository variable `SONAR_PROJECT_KEY`
- SonarCloud project setting: `Automatic Analysis` disabled

Operationally, the SonarCloud project configuration is also the current source of truth for the
existing 80% coverage threshold.

### Update process

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The configuration
  covers `github-actions`, `nuget`, and `npm` ecosystems on staggered monthly/weekly schedules.
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
`actions/dependency-review-action` on every pull request and on merge queue checks
(`merge_group`). It scans manifest and lock file changes for newly introduced vulnerabilities and
fails the check when severity is `moderate` or higher.

This workflow is intentionally separate from the main CI workflow so that its required check
status does not interfere with path-based optimizations in the CI workflow.

The action natively understands `merge_group` payloads, so the same required check name continues
to report correctly when merge queue is enabled.

## Actionlint Workflow

A separate workflow (`.github/workflows/actionlint.yml`) runs Actionlint for changes to
`.github/workflows/**` and `.github/actions/**`.

### Actionlint

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/actionlint.yml` |
| Primary job name | `Actionlint` |
| Runner | `ubuntu-latest` |
| Merge gate | Not required |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Install `shellcheck`
3. Download the pinned `actionlint` release, verify its checksum, and install it locally
4. Run `actionlint` against workflow files and local composite actions

This workflow is intentionally lightweight and targeted. It complements the main CI workflow by
catching workflow syntax, expression, and embedded shell mistakes before a workflow edit breaks the
repository's primary validation path.

## Secret Scan Workflow

A separate workflow (`.github/workflows/secret-scan.yml`) runs lightweight repository secret
scanning using the pinned `gitleaks` release binary.

### Secret Scan

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/secret-scan.yml` |
| Primary job name | `Secret Scan` |
| Runner | `ubuntu-latest` |
| Merge gate | Required |

**Steps:**

1. Checkout repository (`actions/checkout`)
2. Download the pinned `gitleaks` release, verify its checksum, and install it locally
3. Scan the working tree for potential secrets and produce a SARIF report
4. Upload SARIF results to GitHub code scanning using `github/codeql-action/upload-sarif`
5. Fail the workflow if potential secrets are detected

This workflow is intentionally separate from the main CI workflow because secret scanning is a
repository-governance concern rather than an application build/test concern. Keeping it separate
preserves a clear failure signal without duplicating the main validation pipeline.

Unlike the path-scoped governance workflows, `Secret Scan` is a good merge-gate candidate because
it runs on all pull requests and pushes to `main`, has a low runtime cost, and protects against a
high-impact failure mode that should block merges when detected.

`Secret Scan` also runs on `merge_group` so a required merge-queue build reports the same check
name instead of stalling on a missing governance result.

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

| Ecosystem | Scope | Schedule | PR limit | Update shaping |
| --- | --- | --- | --- | --- |
| `github-actions` | Workflow action references | Monthly at 05:00 UTC | 1 | All action updates grouped into one PR |
| `nuget` | .NET package dependencies | Weekly on Tuesday at 05:00 UTC | 3 | Minor/patch updates grouped, security updates grouped, cooldown enabled |
| `npm` | Node.js dependencies | Weekly on Wednesday at 05:00 UTC | 2 | Minor/patch updates grouped, security updates grouped, cooldown enabled |

Dependabot PRs use conventional commit prefixes (`ci` for actions, `deps` for packages).

The repository relies on Dependabot defaults for npm labels and uses explicit custom labels for
ecosystems where the repository wants a different triage taxonomy than the default ecosystem
label. Those custom labels are managed in `.github/labels.json` and synced by
`.github/workflows/sync-labels.yml`.

The configuration intentionally reduces PR churn instead of accepting Dependabot's default
one-PR-per-update behavior:

- `open-pull-requests-limit` keeps concurrent version-update PR volume bounded per ecosystem.
- `groups` consolidate related updates so low-risk churn does not fan out into many small PRs.
- `cooldown` delays fast-follow NuGet and npm version updates, with longer delays for major changes
  than for minor or patch changes.
- Weekly schedules are staggered across ecosystems so update traffic does not land all at once.

Security updates remain intentionally prompt. Cooldown only affects version updates, not security
updates, so vulnerable dependencies can still surface quickly when GitHub identifies an advisory.

## Branch Protection Rules

Branch protection for `main` is configured to require the following status checks:

- `Build and Test` (from `.github/workflows/ci.yml`)
- `Lint` (from `.github/workflows/ci.yml`)
- `Dependency Review` (from `.github/workflows/dependency-review.yml`)
- `Secret Scan` (from `.github/workflows/secret-scan.yml`)
- `SonarCloud` (from `.github/workflows/ci.yml`)

These names match the `name:` fields in the respective workflow files. Any rename of the jobs
must be reflected in branch protection settings.

Representative pull request validation has also been observed successfully with these checks,
including the main CI workflow and its integrated SonarCloud status job, the separate dependency
review workflow, and the separate secret scanning workflow. The `Devcontainer Smoke` workflow
remains supplemental and is not part of the required merge gate.

## Next Required Work

The near-term required CI governance item of consolidating duplicated validation and SonarCloud
work has been completed. Required governance checks now also report on `merge_group`, so the next
focus is to monitor the merged workflow set in normal use and adjust only when there is a concrete
operational need.

## Planned Follow-Up Work

The following follow-up items are planned after the baseline rollout:

- Review which SonarCloud scope settings should remain in-repo versus move to SonarCloud UI when
  plan capabilities allow it
- Keep supplemental, non-required governance workflows under review if merge queue becomes part of
  the normal merge path, but avoid expanding required checks without a concrete value signal
- Multi-OS matrix (not required until a concrete cross-platform requirement appears)

## Related Documentation

- [README — Continuous Integration](../README.md#continuous-integration) - Contributor-facing CI summary
- [Code Quality Tools](CODE_QUALITY.md) - Local linting and formatting tools
- [PBI-2026-03-15-02](backlog/PBI-2026-03-15-02-github-actions-ci-workflow.md) - Original delivery plan
