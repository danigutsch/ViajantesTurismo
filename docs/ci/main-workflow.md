# Main CI workflow

Operational details for the primary GitHub Actions workflow in
`.github/workflows/ci.yml`.

## Workflow jobs

The CI workflow runs on every pull request targeting `main`, every push to `main`, on
merge queue checks (`merge_group`), and on `workflow_dispatch`. It defines multiple test
slices, a SonarCloud aggregation job, a lint job, and the shared change-detection gate.

The workflow-level concurrency policy cancels stale runs for non-`main` refs but preserves
in-flight `main` runs. This keeps pull request iteration responsive while ensuring the
protected branch keeps its post-merge validation history intact.

### Fast Validation

| Attribute | Value |
| --- | --- |
| Job key | `fast-validation` |
| Job name | `Fast Validation` |
| Runner | `ubuntu-24.04` |

**Steps:**

1. Wait for `detect-changes` to complete successfully.
2. Read the `fast_validation_required` decision from `detect-changes`.
3. If only documentation changed, run a lightweight success step so the required
   `Fast Validation` check resolves cleanly without starting the expensive validation
   path.
4. Checkout repository (`actions/checkout`) when fast validation work is required.
5. Configure a repository-local NuGet global-packages path and set up the .NET SDK from
   `global.json` with built-in NuGet caching (`actions/setup-dotnet`) when validation work
   is required.
6. Run `dotnet restore ViajantesTurismo.slnx --locked-mode` when validation work is
   required.
7. Run `dotnet tool restore` when validation work is required.
8. Run `bash scripts/run-ci-test-slice.sh --slice-name "Fast Validation" ...` to execute
   the fast project set with project-scoped build, normalized per-slice timing output,
   machine-readable manifest output, and coverage collection.
9. When validation work fails, create a focused diagnostic summary under
   `TestResults/ci-diagnostics/`.
10. Upload the slice-local test results artifact and upload the focused diagnostics
   artifact when the job fails.

The slice result uploads are intentionally best-effort. If validation fails before those
files exist, CI should report the actual build/test failure instead of adding secondary
artifact-missing noise. The focused diagnostics artifacts remain strict because they are
part of the failure-investigation path.

### Admin Integration Tests

| Attribute | Value |
| --- | --- |
| Job key | `admin-integration-tests` |
| Job name | `Admin Integration Tests` |
| Runner | `ubuntu-24.04` |

This slice runs only when `detect-changes` reports that admin integration-sensitive paths
changed. It restores shared prerequisites, then builds and executes only
`tests/ViajantesTurismo.Admin.IntegrationTests/ViajantesTurismo.Admin.IntegrationTests.csproj`,
then uploads slice-local results and diagnostics.

### Mediator Heavy Tests

| Attribute | Value |
| --- | --- |
| Job key | `mediator-heavy-tests` |
| Job name | `Mediator Heavy Tests` |
| Runner | `ubuntu-24.04` |

This slice runs only when mediator/analyzer/source-generator paths changed. It isolates the
slow mediator-specific test projects so they no longer delay ordinary pull requests that do
not touch that surface.

### Admin System Tests

| Attribute | Value |
| --- | --- |
| Job key | `admin-system-tests` |
| Job name | `Admin System Tests` |
| Runner | `ubuntu-24.04` |

This slice runs only when hosted UI or system-test-sensitive paths changed. It restores
shared prerequisites, builds the system-test project, installs Playwright Chromium only,
executes the system test project, and uploads slice-local results and diagnostics.

NuGet lock files (`packages.lock.json`) are committed for the projects in this repository so
that CI can combine `actions/setup-dotnet` built-in caching with locked-mode restore. This
keeps the dependency graph reproducible across pull requests and merge commits while giving
the cache a stable key source.

For pull requests and pushes that only modify `docs/**`, `README.md`, or
`CONTRIBUTING.md`, the affected validation jobs still run and report successful required
checks through lightweight skip steps, but they skip the expensive restore, build,
Playwright, and test steps. This avoids the pending required-check problem caused by
trigger-level `paths` or `paths-ignore` filters. `Fast Validation` is also path-gated now,
so changes isolated to heavier hosted or mediator-specific surfaces do not automatically
re-run the cheaper fast slice.

The change classification logic is implemented in `scripts/detect-changes.sh`, not inline
in the workflow YAML. If the script cannot determine the diff range reliably, it fails
open by setting all validation outputs to `true` so CI prefers extra work over a false
skip.

SDK bump pull requests must refresh committed `packages.lock.json` files when `global.json`
changes. The repository provides `bash scripts/refresh-sdk-lockfiles.sh` as the canonical
command for that maintenance step.

> **Note:** The CI setup path works around a
> [known SDK bug](https://github.com/dotnet/aspnetcore/issues/65391) where
> `dotnet dev-certs https --trust` exits with code 4 on `ubuntu-24.04` in SDK 10.0.103+
> builds. The setup action uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP
> clients in the test run trust the per-user dev certificate.

### SonarCloud

| Attribute | Value |
| --- | --- |
| Job key | `sonarcloud` |
| Job name | `SonarCloud` |
| Runner | `ubuntu-24.04` |

**Steps:**

1. Wait for all validation slices.
2. If only documentation changed, resolve the required check through a lightweight skip
   step.
3. Checkout repository and validate SonarCloud configuration.
4. Restore repository prerequisites and cache SonarCloud packages.
5. Download the `*-results` artifacts from the test slices that actually ran.
6. Generate the aggregated `sonar-coverage.xml` and HTML coverage report from the slice
   artifacts.
7. Run `bash scripts/run-sonar-analysis.sh` in reuse mode so SonarScanner performs a fresh
   build and end step but does not rerun tests.
8. Publish a GitHub summary from `TestResults/sonar-analysis.log` that includes the
   quality gate status, SonarCloud link, warning count, and captured phase timings, then
   upload the coverage report, `sonar-coverage`, `sonar-analysis-log`, and
   `sonar-analysis-manifest` artifacts.

This job remains the dedicated required `SonarCloud` check, but it now aggregates coverage
from the parallel test slices before performing hosted analysis.

### Lint

| Attribute | Value |
| --- | --- |
| Job key | `lint` |
| Job name | `Lint` |
| Runner | `ubuntu-24.04` |

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

### Recommended target state

The current target state is a single primary validation workflow that:

1. detects docs-only and path-scoped changes
2. runs fast, integration, mediator-heavy, and system-test slices in parallel when needed
3. aggregates coverage from those slices into one hosted SonarCloud analysis job
4. continues to run on pull requests to `main`, pushes to `main`, and manual dispatch
5. adds `merge_group` coverage if merge queue is adopted
6. preserves required check names expected by branch protection
