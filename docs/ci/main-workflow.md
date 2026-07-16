# Main CI workflow

Operational details for the primary GitHub Actions workflow in
`.github/workflows/ci.yml`.

## Workflow jobs

The CI workflow runs on pull request activity (`opened`, `edited`, `synchronize`,
`reopened`, and `ready_for_review`), every push to `main`, on merge queue checks
(`merge_group`), and on `workflow_dispatch`. It defines multiple test slices, a version
calculation job, a SonarCloud aggregation job, a lint job, and the shared change-detection gate.

The workflow-level concurrency policy cancels stale runs for non-`main` refs but preserves
in-flight `main` runs. This keeps pull request iteration responsive while ensuring the
protected branch keeps its post-merge validation history intact.

All jobs except `OpenAPI Tool Windows` use the repository `CI_UBUNTU_RUNNER` Actions variable.
Set that variable to the current repository CI baseline runner. `OpenAPI Tool Windows` uses
`windows-latest` to validate the platform-specific child-process environment allowlist.

> **Note:** When `CI_UBUNTU_RUNNER` points to a preview hosted runner image, queue time
> and image behavior may be less stable than GA runner images.

### Calculate Version

| Attribute | Value |
| --- | --- |
| Job key | `calculate-version` |
| Job name | `Calculate Version` |
| Runner | Repository CI baseline |

This job checks out full history, restores only `SharedKernel.Versioning.Tool`, and runs
`SharedKernel.Versioning.Tool calculate-release`. The tool finds the latest `v*` SemVer tag when one exists;
otherwise it uses `0.1.0` as the base while the repository has no versioned release tags. It feeds
commit messages into `sharedkernel-version compute` and exposes the calculated SemVer, release impact,
package version, assembly version, file version, informational version, base version, source tag, and
raw JSON as job outputs for later release workflow steps.

### Test slices

| Attribute | Value |
| --- | --- |
| Job key | `test-slices` |
| Job name | `Test slices` |
| Runner | Repository CI baseline |

One static matrix owns every Linux test slice. Each row declares its display name, change-detection
output, project-list file, timeout, and whether Playwright installation is needed. The shared job
then performs the same checkout, prerequisite setup, test execution, diagnostics, artifact upload,
and summary steps for every row.

`strategy.fail-fast: false` keeps independent rows running when another row fails, preserving
their artifacts and diagnostics. The matrix does not set `max-parallel` or
`CI_TEST_PROJECT_PARALLELISM`, so job-level and project-level parallelism remain unchanged.

The fast row remains no-host and no-container. The provider/database and full-host Admin API rows
remain separate because DCP runner-capacity isolation was measured to be more reliable than sharing
one lane. Browser/system and tooling-heavy rows retain their dedicated runtime boundaries. The
matrix is an execution mechanism; [ADR-030](../adr/20260629-ci-test-lane-selection.md) remains the
source of the lane-selection policy.

### OpenAPI Tool Windows

| Attribute | Value |
| --- | --- |
| Job key | `openapi-tool-windows` |
| Job name | `OpenAPI Tool Windows` |
| Runner | `windows-latest` |

This job restores the OpenAPI tool test project and its API-generation targets, then runs the
OpenAPI tool tests on Windows. It runs independently of the matrix; `Build and Test` aggregates
both outcomes so the platform-specific child-environment safety check remains part of the required
merge gate without delaying unrelated test slices.

### Build and Test

| Attribute | Value |
| --- | --- |
| Job key | `build-and-test` |
| Job name | `Build and Test` |
| Runner | Repository CI baseline |

This is the required non-secret aggregate in the `main` branch-protection rule. It waits for the
test-slice matrix and Windows OpenAPI validation before succeeding. The component rows and Windows
job remain parallel; the aggregate only reports their combined result. Because it does not need
SonarCloud credentials, it blocks a failing test slice on fork pull requests even when
`SonarCloud` is intentionally skipped.

NuGet lock files (`packages.lock.json`) are committed for the projects in this repository so
that CI can combine `actions/setup-dotnet` built-in caching with locked-mode restore. This
keeps the dependency graph reproducible across pull requests and merge commits while giving
the cache a stable key source.

For pull requests and pushes that only modify `docs/**`, `README.md`, `CONTRIBUTING.md`, or
the small allowlist of low-risk contributor-maintenance scripts in
`scripts/detect-changes.sh` (for example `scripts/lint-all.sh` or
`scripts/validate-commit-message.sh`), the affected matrix rows still run and report successful
outcomes to the required `Build and Test` aggregate through lightweight skip steps,
but they skip the expensive restore, build, Playwright, and test steps. This avoids the pending
required-check problem
caused by trigger-level `paths` or `paths-ignore` filters. The fast row is independently
path-gated, so heavier hosted or tooling changes do not automatically re-run it.

The change classification logic is implemented in `scripts/detect-changes.sh`, not inline
in the workflow YAML. If the script cannot determine the diff range reliably, it fails
open by setting all validation outputs to `true` so CI prefers extra work over a false
skip.

Test-slice project membership is now centralized under `scripts/ci-test-slices/*.txt` so the
restore, build, test, and Sonar coverage inputs for each slice stay aligned instead of
duplicating project lists in multiple workflow locations.

Project order in those slice files is also a scheduling hint for bounded local runs: keep slower
projects first so the parallel test worker pool does not leave long-running projects until the end.

When a slice contains more than one project, `scripts/run-ci-test-slice.sh` builds the selected
test projects through one temporary solution so MSBuild owns graph scheduling. It then uses
`scripts/collect-test-coverage.sh` to run those test projects in parallel up to the runner CPU
count. Set `CI_TEST_PROJECT_PARALLELISM` only when diagnosing local resource pressure or a
runner-specific bottleneck.

Lane placement follows [ADR-030: CI Test Lane Selection](../adr/20260629-ci-test-lane-selection.md).
Benchmark locally with `scripts/benchmark-local-validation.sh` before changing CI slice membership.

Fast-slice membership is intentionally stricter than "small test count." A project that starts an
AppHost, container, database, browser, queue, or other external dependency is dependency-heavy and
must not be added to `scripts/ci-test-slices/fast-validation.txt`. Prefer bundling tests that share a
slow dependency class into an existing dependency-heavy lane before adding another parallel job;
split only after CI timing data shows duplicated setup is cheaper than serial execution.

SDK bump pull requests must refresh committed `packages.lock.json` files when `global.json`
changes. The repository provides `bash scripts/refresh-sdk-lockfiles.sh` as the canonical
command for that maintenance step.

> **Note:** The CI setup path works around a known SDK Linux dev-certs issue where
> `dotnet dev-certs https --trust` can exit with code 4 on Linux CI runners in SDK 10.0.103+
> builds. The setup action uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP
> clients in the test run trust the per-user dev certificate.

### SonarCloud

| Attribute | Value |
| --- | --- |
| Job key | `sonarcloud` |
| Job name | `SonarCloud` |
| Runner | Repository CI baseline |

**Steps:**

1. Wait for all validation slices.
2. If only documentation changed, resolve the required check through a lightweight skip
   step.
3. Checkout repository and validate SonarCloud configuration.
4. Restore repository prerequisites and cache SonarCloud packages.
5. Download all `ci-test-slice-*-results` artifacts from matrix rows that actually ran.
6. Generate the aggregated `sonar-coverage.xml` and HTML coverage report from the slice
   artifacts.
7. Run `bash scripts/run-sonar-analysis.sh` in reuse mode so SonarScanner performs a fresh
   build and end step but does not rerun tests.
8. Publish a GitHub summary from `TestResults/sonar-analysis.log` that includes the
   hosted quality gate status, repository-owned new issue policy status, SonarCloud link,
   warning count, and captured phase timings, then upload the coverage report,
   `sonar-coverage`, `sonar-analysis-log`, and `sonar-analysis-manifest` artifacts.

This job remains the dedicated required `SonarCloud` check, but it now aggregates coverage
from the parallel test slices before performing hosted analysis.

### Lint

| Attribute | Value |
| --- | --- |
| Job key | `lint` |
| Job name | `Lint` |
| Runner | Repository CI baseline |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. On pull requests, detect changed Markdown files and lint only that set with `tj-actions/changed-files` and `DavidAnson/markdownlint-cli2-action`.
3. On non-pull-request runs, lint the full repository Markdown scope with `DavidAnson/markdownlint-cli2-action` using the bundled Node.js runtime.
4. Run `bash scripts/lint-all.sh`.

## Recommended workflow evolution

The previous split between `.github/workflows/ci.yml` and `.github/workflows/sonar.yml`
duplicated expensive setup, build, Playwright installation, test, and coverage work on the
same pull requests and on the subsequent merge commit to `main`.

After reviewing current GitHub Actions and SonarQube Cloud guidance, the repository now
uses the recommended consolidated model: SonarCloud analysis runs inside the main
validation workflow instead of trying to reuse build artifacts across separate workflows.

### Recommendation summary

- keep validation on both pull requests targeting `main` and pushes to `main`
- move SonarScanner for .NET execution into the same workflow that performs build, test,
  and coverage collection
- keep `Lint` as an independent job if separate status visibility remains useful
- add the `merge_group` trigger if merge queue is enabled for the repository
- keep essential SonarCloud configuration in-repo when UI-level administration is limited
  on the current plan, and prefer UI settings only when they are actually available and
  sustainable

### Why this is the recommended direction

For .NET repositories, SonarScanner is designed around a `begin` → `build` →
`test/coverage` → `end` flow. Reusing artifacts from a separate CI workflow would reduce
YAML duplication at best, but it would not remove the requirement to run a Sonar-wrapped
build. Consolidating the build, test, coverage, and Sonar steps into one validation
workflow removes the duplicated runner work while keeping the analysis model aligned with
Sonar's recommended usage.

The second run after merge is still expected and desirable. GitHub Actions treats
`pull_request` and `push` as separate events, so a validation run on the PR and a
follow-up validation run on the merge commit to `main` are normal. The post-merge run
confirms the actual branch state rather than only the pre-merge PR state.

### Operational guidance

- reusable workflows are appropriate for reducing YAML duplication, but they do not
  eliminate the compute cost of repeated build/test execution
- `workflow_run` chaining is not the preferred solution for this repository's SonarCloud
  path because it adds complexity and security considerations without solving the
  SonarScanner for .NET build coupling
- `pull_request_target` should not be used for build, test, or Sonar analysis of
  untrusted pull request code
- existing workflow-level `concurrency` remains appropriate for canceling stale runs on
  the same ref, but `main` should be exempt from cancellation so post-merge validation is
  not interrupted

### Recovery path for missing PR-context checks

See [Governance](governance.md#recovery-for-missing-pr-checks).

### Recommended target state

The current target state is a single primary validation workflow that:

1. detects docs-only and path-scoped changes
2. runs fast, integration, mediator-heavy, and system-test slices in parallel when needed
3. aggregates coverage from those slices into one hosted SonarCloud analysis job
4. continues to run on pull requests to `main`, pushes to `main`, and manual dispatch
5. adds `merge_group` coverage if merge queue is adopted
6. preserves required check names expected by branch protection
