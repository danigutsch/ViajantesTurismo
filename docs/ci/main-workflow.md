# Main CI workflow

Operational details for the primary GitHub Actions workflow in
`.github/workflows/ci.yml`.

## Workflow jobs

The CI workflow runs on every pull request targeting `main`, every push to `main`, on
merge queue checks (`merge_group`), and on `workflow_dispatch`. It defines three primary
jobs plus the shared change-detection gate.

The workflow-level concurrency policy cancels stale runs for non-`main` refs but preserves
in-flight `main` runs. This keeps pull request iteration responsive while ensuring the
protected branch keeps its post-merge validation history intact.

### Build and Test

| Attribute | Value |
| --- | --- |
| Job key | `build-and-test` |
| Job name | `Build and Test` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Read the `build_required` decision from `detect-changes`.
2. If only documentation changed, run a lightweight success step so the required
   `Build and Test` check resolves cleanly without starting the expensive validation
   path.
3. Validate that the required SonarCloud secret and repository variables exist before
   the expensive validation path starts.
4. Checkout repository (`actions/checkout`) when build/test work is required.
5. Configure a repository-local NuGet global-packages path and set up the .NET SDK from
  `global.json` with built-in NuGet caching (`actions/setup-dotnet`) when build/test work
  is required.
6. Set up Node.js from `.nvmrc` (`actions/setup-node`) when build/test work is required.
7. Run `dotnet restore ViajantesTurismo.slnx --locked-mode` when build/test work is
  required.
8. Run `dotnet tool restore` when build/test work is required.
9. Cache SonarCloud packages under `~/.sonar/cache` when validation work is required.
10. Run `bash scripts/run-sonar-analysis.sh` when validation work is required. This script
   wraps the SonarScanner for .NET `begin` / `build` / `coverage collection` /
  `coverage conversion` / `end` flow and produces both HTML coverage output and the
  SonarQube XML coverage input.
11. Publish a GitHub Actions job summary from `TestResults/sonar-analysis.log` so the
  quality gate status, SonarCloud link, and any parse warnings appear on the workflow
  run summary page without opening the full log.
12. When validation work fails, create a focused diagnostic summary under
    `TestResults/ci-diagnostics/`.
13. Upload test result artifacts, HTML coverage artifacts, the Sonar coverage input
  artifact, and the raw Sonar analysis log artifact (`actions/upload-artifact`, runs on
  `always()` after the validation step executes) when validation ran.
14. Upload the focused `build-test-diagnostics` artifact when the job fails.

The `test-results`, `coverage-report`, `sonar-coverage`, and `sonar-analysis-log`
uploads are intentionally best-effort. If validation fails before those files exist, CI
should report the actual build/test/Sonar failure instead of adding secondary
artifact-missing noise. The focused diagnostics artifact remains strict because it is
part of the failure-investigation path.

The SonarCloud configuration preflight intentionally runs before restore, tool setup, and
analysis so missing repository settings fail in a dedicated step with an actionable
annotation instead of surfacing later as an opaque shell exit.

NuGet lock files (`packages.lock.json`) are committed for the projects in this repository so
that CI can combine `actions/setup-dotnet` built-in caching with locked-mode restore. This
keeps the dependency graph reproducible across pull requests and merge commits while giving
the cache a stable key source.

For pull requests and pushes that only modify `docs/**`, `README.md`, or
`CONTRIBUTING.md`, the job still runs and reports a successful `Build and Test` check
through a lightweight docs-only step, but it skips the expensive restore, build,
Playwright, and test steps. This avoids the pending required-check problem caused by
trigger-level `paths` or `paths-ignore` filters.

The change classification logic is implemented in `scripts/detect-changes.sh`, not inline
in the workflow YAML. If the script cannot determine the diff range reliably, it fails
open by setting `build_required=true` so CI prefers extra work over a false skip.

> **Note:** The CI setup path works around a
> [known SDK bug](https://github.com/dotnet/aspnetcore/issues/65391) where
> `dotnet dev-certs https --trust` exits with code 4 on `ubuntu-latest` in SDK 10.0.103+
> builds. The setup action uses `|| true` to tolerate the non-zero exit and then sets
> `SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust` via `$GITHUB_ENV` so that .NET HTTP
> clients in the test run trust the per-user dev certificate.

### SonarCloud

| Attribute | Value |
| --- | --- |
| Job key | `sonarcloud` |
| Job name | `SonarCloud` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Wait for `build-and-test`.
2. Report a distinct `SonarCloud` required check result without rerunning build, test, or
   analysis.

This job is intentionally lightweight. It exists so branch protection can keep a dedicated
`SonarCloud` check name while the actual Sonar analysis runs only once inside the
`Build and Test` job.

### Lint

| Attribute | Value |
| --- | --- |
| Job key | `lint` |
| Job name | `Lint` |
| Runner | `ubuntu-latest` |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. Set up Node.js from `.nvmrc` with npm cache (`actions/setup-node`).
3. Run `npm ci --ignore-scripts`.
4. Run `npm run lint:all`.

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

The intended end state is a single primary validation workflow that:

1. detects docs-only changes
2. runs build, Playwright setup, tests, coverage, and Sonar analysis in one build pipeline
   when validation work is required
3. continues to run on pull requests to `main`, pushes to `main`, and manual dispatch
4. adds `merge_group` coverage if merge queue is adopted
5. preserves required check names expected by branch protection
