# Main CI workflow

Operational details for the primary GitHub Actions workflow in
`.github/workflows/ci.yml`.

## Workflow jobs

The CI workflow runs on pull request activity (`opened`, `labeled` for explicit `ci:rerun`, `synchronize`,
`reopened`, and `ready_for_review`), every push to `main`, on merge queue checks
(`merge_group`), and on `workflow_dispatch`. It defines multiple test slices, a version
calculation job, a repository-owned coverage aggregation job, a lint job, and the shared change-detection gate.

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

For build-relevant changes, this job waits for selection, checks out full history, restores only `SharedKernel.Versioning.Tool`, and runs
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
job remain parallel; the aggregate only reports their combined result. It uses no analysis secrets and blocks a failing test slice on fork pull requests.

NuGet lock files (`packages.lock.json`) are committed for the projects in this repository so
that CI can combine `actions/setup-dotnet` built-in caching with locked-mode restore. This
keeps the dependency graph reproducible across pull requests and merge commits while giving
the cache a stable key source.

Standalone documentation changes skip the entire test matrix, Windows tests, version calculation,
and solution build. Required `Build and Test` and `Quality Gate` still report successful results
from validated selection evidence. `Lint` validates Markdown, links, and line endings without
running tests. Contributor-maintenance scripts retain repository-tool tests even when they do not
select application tests. Trigger-level `paths` or `paths-ignore` filters are not used for required
checks. Every build-relevant project change still selects the Architecture Tests sentinel and
activates both fast shards; dependency-heavy rows retain their graph-derived gates.

The required change-classification path is the `select-ci-test-projects` command in
`ViajantesTurismo.RepoConfig.Tool`. It maps changed files to owning solution projects, follows reverse
transitive `ProjectReference` dependencies, and maps affected tests back to the fixed slice
manifests. Missing ranges, unknown paths, unresolved references, and malformed graphs fail open to
full validation. Pull requests use a merge-base range; main pushes compare exact before/after
revisions. Merge-queue and manual runs validate all slices.

The generated project-level plan is uploaded as `ci-selected-test-projects` for timing and coverage
evaluation. CI currently uses it to decide which complete slices run, while each active row still
executes its checked-in source manifest. Changed-code coverage is checked against the artifacts from those active slices;
full-run coverage trends are kept separate from partial selections.

Test-slice project membership is now centralized under `scripts/ci-test-slices/*.txt` so the
restore, build, test, and coverage inputs for each slice stay aligned instead of
duplicating project lists in multiple workflow locations.

Keep all six canonical slice files when extracting SharedKernel into its own repository. A slice
may become empty after its last test project leaves the application solution; selection emits
`<slice>_required=false`, and the existing matrix skip step reports success without restoring or
running that slice. Every xUnit project still in the application solution must remain assigned
exactly once. Emptying a manifest while leaving its projects in the solution fails membership
validation; missing or unexpected slice files also remain errors.

Multi-project slice manifests restore through one temporary solution. This lets MSBuild schedule
the selected restore graph in parallel while preserving locked-mode restore and slice isolation.
Jobs that invoke only project-based tools skip `dotnet tool restore`; coverage aggregation and release
preparation retain local-tool restore because they use manifest tools.

Project order in those slice files is also a scheduling hint for bounded local runs: keep slower
projects first so the parallel test worker pool does not leave long-running projects until the end.

Fast Validation is split into two fixed 28-project execution shards. Run `30085894084` measured the
former single row at about 138 seconds of setup plus 518 seconds for build, OpenAPI preparation, and
coverage tests. The split duplicates setup but allows those phases to proceed on two runners. Each
shard uses its own `fast_validation_1_required` or `fast_validation_2_required` selection flag.
The `Build and Test` aggregate and coverage artifact pattern continue to represent one logical lane.

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
`dotnet restore ViajantesTurismo.slnx --force-evaluate`, then
`dotnet restore ViajantesTurismo.slnx --locked-mode`. CI uses locked restore to reject drift; contributors
must review regenerated locks and follow the canonical
[dependency graph and lock-file maintenance workflow](../../CONTRIBUTING.md#dependency-graph-and-lock-file-maintenance).

> **Note:** The CI setup path works around a known SDK Linux dev-certs issue where
> `dotnet dev-certs https --trust` can exit with code 4 on Linux CI runners in SDK 10.0.103+
> builds. The setup action uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP
> clients in the test run trust the per-user dev certificate.

### Quality Gate

| Attribute | Value |
| --- | --- |
| Job key | `quality-gate` |
| Job name | `Quality Gate` |
| Runner | Repository CI baseline |

The job requires successful change detection and test slices. For build-relevant changes it
merges selected slice Cobertura reports with ReportGenerator and uploads `quality-evidence`.
Pull requests and merge groups run the C# `verify-coverage` command against a zero-context diff,
enforcing 80% combined coverage of changed executable lines and conditions. Missing or malformed
evidence fails. Full runs publish trends without pretending to enforce a global 80% threshold.
Documentation-only changes use the lightweight success path.

This job requires no Sonar token or other analysis secret and runs for fork pull requests.
See [quality gates](quality-gates.md) for analysis gaps, child-process coverage limitations,
and the required remote branch-protection transition.

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
4. For documentation-only changes, check line endings and local links without running tests.
5. For other changes, run `bash scripts/lint-all.sh`, including its repository-tool tests.

## Workflow evolution

Validation remains consolidated in one primary workflow: select changes, execute independent
slices, aggregate coverage, and report required checks. The historical hosted Sonar analysis
has been replaced by repository-owned SDK analysis and changed-code coverage; no scanner-wrapped
rebuild or hosted analysis credentials are needed.

Pull-request validation and post-merge `main` validation serve separate purposes: the latter
checks the actual merged branch. Keep `merge_group` enabled and preserve in-flight `main` runs.
Never use `pull_request_target` to execute untrusted pull-request build or test code.

A maintainer must update the remote required check from `SonarCloud` to `Quality Gate` only
after the replacement passes. Workflow edits alone do not change branch protection.

For missing PR-context checks, see [Governance](governance.md#recovery-for-missing-pr-checks).

## Parallel solution-build evidence

`Solution Build` runs after change detection alongside test slices. It validates the complete
solution, including projects outside test dependency graphs, and uploads revision-bound Release
symbols as `solution-build-evidence`. `Quality Gate` checks the solution-build result, downloads and
verifies those symbols, and restores only the reporting/repository tools. It no longer builds the
full solution after tests. Portable-PDB document checks retain their existing coverage semantics.

The `run-ci-test-slice` application command owns graph creation, OpenAPI preparation, Playwright
selection, timing logs, and failure manifests. The Bash entrypoint delegates to that command;
existing coverage collection and browser installation remain platform adapters.
