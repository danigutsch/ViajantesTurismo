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
6. `dotnet build ViajantesTurismo.slnx --no-restore` when build/test work is required
7. Install Playwright browsers and OS dependencies (`playwright.ps1 install --with-deps`, located dynamically via `find`) when build/test work is required
8. Trust HTTPS developer certificate and set `SSL_CERT_DIR` when build/test work is required
9. `dotnet test --solution ViajantesTurismo.slnx --no-build` when build/test work is required
10. Upload test result artifacts (`actions/upload-artifact`, runs on `always()` after the test step executes) when tests ran

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
3. `npm ci`
4. `npm run lint:all`

## Artifacts

Test result artifacts are uploaded by the `build-and-test` job unconditionally (`if: always()`).

| Artifact name | Contents | Retention |
| --- | --- | --- |
| `test-results` | `**/TestResults/**` from all test projects | 7 days |

The artifact includes per-project `TestResults` folders, which contain `.trx` result files and
`coverage.cobertura.xml` when coverage collection is enabled.
If the test step ran, missing result files are treated as an error because that indicates the
test infrastructure did not produce the expected outputs.

Artifact scope is kept narrow — only test outputs that materially help diagnose failures are
included. Do not broaden the upload glob without a clear reason.

## Reproducing Failures Locally

All CI commands map directly to local developer commands.

### Build and Test job

```bash
# From repository root
dotnet restore ViajantesTurismo.slnx
dotnet build ViajantesTurismo.slnx --no-restore
pwsh $(find tests -name playwright.ps1 -path "*/bin/Debug/*" | head -1) install --with-deps
dotnet dev-certs https --trust || true
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"
dotnet test --solution ViajantesTurismo.slnx --no-build
```

For documentation-only changes (`docs/**`, `README.md`, or `CONTRIBUTING.md`), CI skips the
build-and-test commands above but still records a successful `Build and Test` check.

### Lint job

```bash
# From repository root
npm ci
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

These names match the `name:` fields in `.github/workflows/ci.yml` and
`.github/workflows/dependency-review.yml`. Any rename of the jobs must be reflected in branch
protection settings.

## Action Versioning Policy

All GitHub Actions used in this workflow are pinned to **major version tags**
(for example `actions/checkout@v6`). This is the initial baseline policy.

### Rationale

Major version tags offer a practical balance between stability and security for the early
lifecycle of this workflow:

- The risk surface from a compromised major-version tag is accepted as reasonable given
  that these are all official GitHub-maintained actions.
- Major version tags receive patch and minor fixes automatically, including security patches.
- Pinning to immutable SHAs provides stronger supply-chain guarantees but increases
  maintenance burden; that trade-off is deferred until the workflow has proven stable.

### Trusted actions

The workflow uses only official GitHub-maintained actions:

| Action | Purpose |
| --- | --- |
| `actions/checkout@v6` | Repository checkout |
| `actions/setup-dotnet@v5` | .NET SDK provisioning |
| `actions/setup-node@v6` | Node.js provisioning |
| `actions/upload-artifact@v7` | Test result artifact upload |
| `actions/dependency-review-action@v4` | PR dependency and license review |

Before adding any third-party action, document the trust decision and update this table.

### Update process

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The configuration
  covers `github-actions`, `nuget`, and `npm` ecosystems on a weekly schedule.
- When upgrading a major version (for example `@v6` to `@v7`), review the migration guide and
  verify the workflow still passes before merging.

### Future migration to SHA pinning

If the project adopts a stricter supply-chain posture, migrate to immutable SHAs and introduce
a documented process for keeping those SHAs current (for example, using a tool such as
[`pin-github-action`](https://github.com/mheap/pin-github-action) or Dependabot SHA pinning).

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

Branch protection for `main` must be configured manually in the GitHub repository settings.
The following status checks should be required:

- `Build and Test` (from `.github/workflows/ci.yml`)
- `Lint` (from `.github/workflows/ci.yml`)
- `Dependency Review` (from `.github/workflows/dependency-review.yml`)

These names match the `name:` fields in the respective workflow files. Any rename of the jobs
must be reflected in branch protection settings.

> **Note:** This is a manual GitHub UI configuration step. Navigate to
> **Settings → Branches → Branch protection rules → Add rule** for the `main` branch, then
> enable **Require status checks to pass before merging** and add the check names above.

## Next Required Work

The near-term required items from the initial rollout are complete. Monitor the workflow in
normal use and move deferred items into scope only when there is a concrete operational need.

## Deferred Work

The following items remain out of scope until a concrete need or prerequisite is met:

- SHA pinning for actions (see [Action Versioning Policy](#action-versioning-policy) above)
- Coverage report generation and upload as a separate artifact
- Scheduled devcontainer smoke validation
- Multi-OS matrix (not required until a concrete cross-platform requirement appears)
- Coverage thresholds (not enforced until baseline coverage trends are established)

## Related Documentation

- [README — Continuous Integration](../README.md#continuous-integration) - Contributor-facing CI summary
- [Code Quality Tools](CODE_QUALITY.md) - Local linting and formatting tools
- [PBI-2026-03-15-02](backlog/PBI-2026-03-15-02-github-actions-ci-workflow.md) - Original delivery plan
