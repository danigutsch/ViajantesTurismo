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
| Job name | Matrix row `display_name` |
| Runner | Repository CI baseline |

One static matrix owns every Linux test slice. Each row declares its display name, change-detection
output, project-list file, timeout, and whether Playwright installation is needed. The shared job
then performs the same checkout, prerequisite setup, test execution, diagnostics, artifact upload,
and summary steps for every row.

`strategy.fail-fast: false` keeps independent rows running when another row fails, preserving
their artifacts and diagnostics. The matrix does not set `max-parallel` or
`CI_TEST_PROJECT_PARALLELISM`, so job-level and project-level parallelism remain unchanged.

The two Fast Validation rows form one logical no-host/no-container lane and share the same change
gate. The provider/database and full-host Admin API rows
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

For pull requests that only modify `docs/**`, `README.md`, `CONTRIBUTING.md`, or the small
allowlist of low-risk contributor-maintenance files, the matrix rows still run and report successful
outcomes to the required `Build and Test` aggregate through lightweight skip steps,
but they skip the expensive restore, build, Playwright, and test steps. This avoids the pending
required-check problem
caused by trigger-level `paths` or `paths-ignore` filters. The logical fast lane is independently
path-gated, but every build-relevant project change also selects the Architecture Tests sentinel.
This activates both complete fast shards for structural validation while dependency-heavy rows use
their own graph-derived gates.

The required change-classification path is the `select-ci-test-projects` command in
`SharedKernel.RepoConfig.Tool`. It maps changed files to owning solution projects, follows reverse
transitive `ProjectReference` dependencies, and maps affected tests back to the fixed slice
manifests. Missing ranges, unknown paths, unresolved references, and malformed graphs fail open to
full validation. Pull requests use a merge-base range; `main`, merge-queue, and manual runs validate
all slices.

The generated project-level plan is uploaded as `ci-selected-test-projects` for timing and coverage
evaluation. CI currently uses it to decide which complete slices run, while each active row still
executes its checked-in source manifest. This preserves repository-wide Sonar coverage semantics;
project-level enforcement remains deferred until selected coverage can be combined without making
unaffected source appear uncovered.

Test-slice project membership is now centralized under `scripts/ci-test-slices/*.txt` so the
restore, build, test, and Sonar coverage inputs for each slice stay aligned instead of
duplicating project lists in multiple workflow locations.

Multi-project slice manifests restore through one temporary solution. This lets MSBuild schedule
the selected restore graph in parallel while preserving locked-mode restore and slice isolation.
Jobs that invoke only project-based tools skip `dotnet tool restore`; SonarCloud and release
preparation retain local-tool restore because they use manifest tools.

Project order in those slice files is also a scheduling hint for bounded local runs: keep slower
projects first so the parallel test worker pool does not leave long-running projects until the end.

Fast Validation is split into two fixed 28-project execution shards. Run `30085894084` measured the
former single row at about 138 seconds of setup plus 518 seconds for build, OpenAPI preparation, and
coverage tests. The split duplicates setup but allows those phases to proceed on two runners while
the stable `fast_validation_required` gate, `Build and Test` aggregate, and Sonar artifact pattern
continue to represent one logical lane.

The shard containing all three HTTP contract projects prepares Admin, Catalog, and Branding OpenAPI
artifacts through one parallel MSBuild graph. Focused local slices that contain only a subset keep the
individual generation commands.

When a slice contains more than one project, `scripts/run-ci-test-slice.sh` builds the selected
test projects through one temporary solution so MSBuild owns graph scheduling. It then uses
`scripts/collect-test-coverage.sh` to run those test projects in parallel up to the runner CPU
count. Set `CI_TEST_PROJECT_PARALLELISM` only when diagnosing local resource pressure or a
runner-specific bottleneck.

Lane placement follows [ADR-030: CI Test Lane Selection](../adr/20260629-ci-test-lane-selection.md).
Benchmark locally with `scripts/benchmark-local-validation.sh` before changing CI slice membership.

Fast-slice membership is intentionally stricter than "small test count." A project that starts an
AppHost, container, database, browser, queue, or other external dependency is dependency-heavy and
must not be added to `scripts/ci-test-slices/fast-validation-*.txt`. Prefer bundling tests that share a
slow dependency class into an existing dependency-heavy lane before adding another parallel job;
split only after CI timing data shows duplicated setup is cheaper than serial execution.

Dependency-graph changes—including NuGet, SDK, local-tool, project-reference, or dependency-related
rebase changes—must refresh committed `packages.lock.json` files with
`bash scripts/refresh-dependency-lockfiles.sh`. CI uses locked restore to reject drift; contributors
must review regenerated locks and follow the canonical
[dependency graph and lock-file maintenance workflow](../../CONTRIBUTING.md#dependency-graph-and-lock-file-maintenance).

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
2. For a documentation-only pull request, resolve the required check through a lightweight skip
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
