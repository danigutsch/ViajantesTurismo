# Main CI workflow

Operational details for the primary GitHub Actions workflow in
`.github/workflows/ci.yml`.

## Workflow jobs

The CI workflow runs on pull request activity (`opened`, `labeled` for explicit `ci:rerun`, `synchronize`,
`reopened`, and `ready_for_review`), every push to `main`, on merge queue checks
(`merge_group`), on a nightly `schedule`, and on `workflow_dispatch`. It defines multiple test slices, a version
calculation job, a solution build, a changed-code coverage gate, a lint job, and the shared change-detection gate.

The workflow-level concurrency policy cancels stale runs for non-`main` refs but preserves
in-flight `main` runs. This keeps pull request iteration responsive while ensuring the
protected branch keeps its post-merge validation history intact.

All jobs use the repository `CI_UBUNTU_RUNNER` Actions variable.
Set that variable to the current repository CI baseline runner.

> **Note:** When `CI_UBUNTU_RUNNER` points to a preview hosted runner image, queue time
> and image behavior may be less stable than GA runner images.

### Calculate Version

| Attribute | Value |
| --- | --- |
| Job key | `calculate-version` |
| Job name | `Calculate Version` |
| Runner | Repository CI baseline |

For build-relevant changes, this job waits for selection, checks out full history, stages the pinned SharedKernel
candidate through `setup-ci-prerequisites`, restores the local tools, and runs
`dotnet tool run sharedkernel-version -- calculate-release` with its values passed through `env:`. The tool finds the latest `v*` SemVer tag when one exists;
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

One static matrix owns every Linux test slice. Each row declares its slice `id`, display name, and
timeout; the `id` values equal the `slices[].name` values of `.config/sharedkernel-ci.json`, and an
architecture test keeps them equal. Each row runs only when its `<id>_required` selection output is
`true`. The shared job then performs the same checkout, prerequisite setup, selection download, and
test execution for every row:

```bash
dotnet run --project "$SHAREDKERNEL_SOURCE/tools/SharedKernel.CI.Tool" -c Release --no-build -- \
  run --slice "$SLICE" --coverage --install-playwright --playwright-dependencies --github-summary "$GITHUB_STEP_SUMMARY"
```

`run` restores, builds, and tests the slice's selected test projects through one solution filter,
generates the slice's OpenAPI targets, installs the slice's Playwright browsers, and writes one
Cobertura report per test project. A failed row prints `dotnet --info` and every row uploads
`ci-test-slice-<id>-results`.

`strategy.fail-fast: false` keeps independent rows running when another row fails, preserving
their artifacts and diagnostics. The matrix does not set `max-parallel`, and `run` uses native MTP
module scheduling with no module-parallelism cap.

The two Fast Validation rows form one logical no-host/no-container lane. The provider/database and full-host Admin API rows
remain separate because DCP runner-capacity isolation was measured to be more reliable than sharing
one lane. Browser/system and tooling-heavy rows retain their dedicated runtime boundaries. The
matrix is an execution mechanism; [ADR-030](../adr/20260629-ci-test-lane-selection.md) remains the
source of the lane-selection policy.

### Build and Test

| Attribute | Value |
| --- | --- |
| Job key | `build-and-test` |
| Job name | `Build and Test` |
| Runner | Repository CI baseline |

This is the required non-secret aggregate in the `main` branch-protection rule. It waits for the
test-slice matrix before succeeding. The component rows remain parallel; the aggregate only reports
their combined result. It uses no analysis secrets and blocks a failing test slice on fork pull requests.

NuGet lock files (`packages.lock.json`) are committed for the projects in this repository so
that CI can combine `actions/setup-dotnet` built-in caching with locked-mode restore. This
keeps the dependency graph reproducible across pull requests and merge commits while giving
the cache a stable key source.

Documentation-only changes skip the entire test matrix, version calculation, and solution build.
Required `Build and Test` and `Quality Gate` still report successful results from validated selection
evidence. `Lint` validates Markdown, links, and line endings without running tests. Trigger-level
`paths` or `paths-ignore` filters are not used for required checks.

### Detect Changes

The required change classification is `sharedkernel-ci select`, built from the pinned SharedKernel
source by `.github/actions/build-sharedkernel-tool` before any candidate is staged. It reads
`.config/sharedkernel-ci.json` and compares a base revision with the checkout:

| Event | Base |
| --- | --- |
| `pull_request` | the first parent of the pull request merge commit |
| `push` | `github.event.before` |
| `merge_group` | `github.event.merge_group.base_sha` |
| `schedule`, `workflow_dispatch` | none, so every test project is selected |

The nightly schedule therefore runs every slice on `main`. `select` classifies each changed path:
`fullValidation` patterns (workflows, actions, `.config`, build props and targets, the solution,
SDK and NuGet configuration, `scripts`, and `setup-dev.*`) select every test project; documentation
patterns (Markdown and images under `docs`, the root `README.md`, `AGENTS.md`, `CONTRIBUTING.md`,
`SECURITY.md`, `SUPPORT.md`, and `LICENSE.txt`, roadmap items, order, themes, and README,
`observability/README.md`, repository metadata files such as `CODEOWNERS` and `.gitignore`, the
test guides under `tests`, `tests/performance`, `docs/architecture/*.json`, and editor settings) select nothing while
every tracked build file is inert; `testPaths` rules send the roadmap configuration, schemas and reconciliation to
`ViajantesTurismo.ArchitectureTests`, `observability` configuration to the architecture and hosted tests, and
`tests/fixtures/media-accessibility` to `ViajantesTurismo.Catalog.UnitTests`; any other path selects the test projects
that depend on its owning project through `ProjectReference`. A path without an owning project, an unreadable or
uncertain project graph, a missing or non-ancestor base,
and a changed project that no selected test project covers (`requireCoveringTests`) select every test project.

`alwaysSelected` adds `ViajantesTurismo.ArchitectureTests` and the Admin, Branding, and Catalog
contract tests whenever a change selects any project. The contract tests are always selected because
the `fast-validation-1` slice generates the three OpenAPI targets whenever it runs, and generation
needs the API projects that only those tests restore.

`select` writes `TestResults/selection/selection.json` with the reasons, a solution filter per
required slice, and `build.slnf`; the job uploads the directory as `ci-selection`, and the test rows
run the projects it lists. Changed-code coverage is checked against the reports of those selected
projects.

### Slice membership

`.config/sharedkernel-ci.json` lists every test project of the solution in exactly one slice;
`select` fails when a solution test project is unlisted or listed twice. A slice runs only its
selected test projects. Keep the five slices; a slice that selects nothing for a change reports
`<slice>_required=false`, and the matrix skip step reports success without restoring or running it.

Fast Validation is split into two fixed execution shards that share setup cost across two runners.
The `fast-validation-1` slice holds all three HTTP contract projects and generates the Admin,
Branding, and Catalog OpenAPI artifacts after its build. The `hosted-tests` slice installs Chromium
with its system dependencies; on Ubuntu 26.04 the job sets `PLAYWRIGHT_HOST_PLATFORM_OVERRIDE` to the
Ubuntu 24.04 platform because Playwright 1.60 has no Ubuntu 26.04 manifests yet.

Lane placement follows [ADR-030: CI Test Lane Selection](../adr/20260629-ci-test-lane-selection.md).
Measure locally with `dotnet tool run sharedkernel-ci -- run --slice <name>` and its phase timings before
changing CI slice membership.

Fast-slice membership is intentionally stricter than "small test count." A project that starts an
AppHost, container, database, browser, queue, or other external dependency is dependency-heavy and
must not be added to the `fast-validation-*` slices. Prefer bundling tests that share a
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

The job requires successful change detection, test slices, and solution build. For build-relevant
pull requests and merge groups it builds the pinned `sharedkernel-ci` from source, downloads the solution
build evidence and every `ci-test-slice-*-results` artifact, and runs one check:

```bash
dotnet run --project "$SHAREDKERNEL_SOURCE/tools/SharedKernel.CI.Tool" -c Release --no-build -- \
  coverage verify --report artifacts --base "$BASE_SHA" --symbols TestResults/symbol-evidence.json
```

`BASE_SHA` is the Detect Changes base. The check enforces `coverage.thresholdPercent` (80%) of the
changed executable lines and conditions of files that match `coverage.include` and no
`coverage.exclude` in `.config/sharedkernel-ci.json`, and uploads `TestResults/coverage-gate.json` as
`quality-evidence`. Missing or malformed evidence fails. Pushes, scheduled, and manual runs do not
enforce a changed-code threshold. Documentation-only changes use the lightweight success path.

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

Documentation-only and code changes run the same steps; the job does not wait for Detect Changes.

1. Checkout repository (`actions/checkout`, `persist-credentials: false`).
2. On pull requests, detect changed Markdown files and lint only that set with `tj-actions/changed-files` and `DavidAnson/markdownlint-cli2-action`.
3. On non-pull-request runs, lint the full repository Markdown scope with `DavidAnson/markdownlint-cli2-action` using the bundled Node.js runtime.
4. Stage the pinned candidate and restore local tools with `setup-ci-prerequisites`.
5. Install shellcheck from the runner's package feed and lint `.devcontainer/*.sh`, `scripts/*.sh`, and `setup-dev.sh` with `.shellcheckrc`.
6. Run `sharedkernel-repo verify-text-encoding`, `verify-line-endings`, `verify-json`, `verify-links`, `verify-gherkin`,
   and `verify` (roadmap structure, schema, and reciprocal blocker links).
7. Run `sharedkernel-docs verify` (generated-diagram drift) and `sharedkernel-docs check` (documentation conformance).

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

`Solution Build` runs after change detection alongside test slices. It restores and builds the
complete solution, including projects outside test dependency graphs, runs `sharedkernel-ci coverage
record-symbols`, and uploads `TestResults/symbol-evidence.json` with the Release PDBs under `src` as
`solution-build-evidence`. `Quality Gate` verifies those revision-bound symbols inside `coverage verify`;
it does not rebuild the full solution after tests. A changed file that no report lists passes only when
a recorded portable PDB maps no executable code to it.

The job saves the locked restore output and runs `sharedkernel-ci verify-restore-warnings` on it before the build.
A high or critical NuGet vulnerability advisory (`NU1903`, `NU1904`) fails the job and names each package and version;
lower severities and other NuGet warnings stay advisory. `sharedkernel-ci warning-summary` then writes the
blocking and advisory totals to the job summary, also when the verification fails. Restore itself is unchanged, so
`--locked-mode` still rejects drift.
