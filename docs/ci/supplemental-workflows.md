# Supplemental workflows

This document covers the repository's governance and environment-parity workflows that run
outside the main `.github/workflows/ci.yml` validation path.

For the current repository-wide CI tool acquisition baseline, see
[`security-hardening.md`](security-hardening.md).

For the repository-wide workflow trust model, see
[`trust-boundaries.md`](trust-boundaries.md).

Supplemental GitHub-hosted Linux workflows use the repository `CI_UBUNTU_RUNNER`
Actions variable. Set that variable to the current repository CI baseline runner.

> **Note:** When `CI_UBUNTU_RUNNER` points to a preview hosted runner image, queue time
> and image behavior may be less stable than GA runner images.

## Dependency review workflow

A separate workflow (`.github/workflows/dependency-review.yml`) runs the
`actions/dependency-review-action` on every pull request and on merge queue checks
(`merge_group`). It scans manifest and lock file changes for newly introduced
vulnerabilities and fails the check when severity is `moderate` or higher.

This workflow is intentionally separate from the main CI workflow so that its required
check status does not interfere with path-based optimizations in the CI workflow.

The action natively understands `merge_group` payloads, so the same required check name
continues to report correctly when merge queue is enabled.

## Actionlint workflow

A separate workflow (`.github/workflows/actionlint.yml`) runs Actionlint for changes to
`.github/workflows/**` and `.github/actions/**`.

### Actionlint

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/actionlint.yml` |
| Primary job name | `Actionlint` |
| Runner | Repository CI baseline |
| Merge gate | Not required |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. Install `shellcheck`.
3. Download the pinned `actionlint` release, verify its checksum, and install it locally.
4. Run `actionlint` against workflow files and local composite actions.

This workflow is intentionally lightweight and targeted. It complements the main CI
workflow by catching workflow syntax, expression, and embedded shell mistakes before a
workflow edit breaks the repository's primary validation path.

## Secret scan workflow

A separate workflow (`.github/workflows/secret-scan.yml`) runs lightweight repository
secret scanning using the pinned `gitleaks` release binary.

### Secret Scan

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/secret-scan.yml` |
| Primary job name | `Secret Scan` |
| Runner | Repository CI baseline |
| Merge gate | Required |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. Download the pinned `gitleaks` release, verify its checksum, and install it locally.
3. Scan the working tree for potential secrets and produce a SARIF report.
4. Upload the SARIF file as a regular workflow artifact from the read-only scan job.
5. Publish SARIF results to GitHub code scanning from a dedicated follow-up job that has
   `security-events: write`.
6. Skip the code-scanning upload for fork pull requests where the token cannot write
   `security-events`, while still preserving the artifact.
7. Fail the scan job if potential secrets are detected.

This workflow is intentionally separate from the main CI workflow because secret scanning
is a repository-governance concern rather than an application build/test concern. Keeping
it separate preserves a clear failure signal without duplicating the main validation
pipeline.

For pull requests from forks, GitHub downgrades `GITHUB_TOKEN` permissions and does not
allow the workflow to publish code-scanning SARIF results with `security-events: write`.
The scan job keeps the SARIF file as a normal artifact, and the dedicated upload job is
skipped in that case. This also keeps least-privilege boundaries tighter because the
scan/install path itself only needs read-only repository access.

Unlike the path-scoped governance workflows, `Secret Scan` is a good merge-gate candidate
because it runs on all pull requests and pushes to `main`, has a low runtime cost, and
protects against a high-impact failure mode that should block merges when detected.

`Secret Scan` also runs on `merge_group` so a required merge-queue build reports the same
check name instead of stalling on a missing governance result.

## Devcontainer smoke workflow

A separate workflow (`.github/workflows/devcontainer-smoke.yml`) runs supplemental
devcontainer validation on a weekly schedule, on a monthly deeper-validation schedule, on
manual dispatch, and on pushes to `main` that touch devcontainer and bootstrap inputs such
as `.devcontainer/**`, `global.json`, or dependency manifests for NuGet
packages.

### Devcontainer Smoke

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/devcontainer-smoke.yml` |
| Primary job name | `Devcontainer Smoke` |
| Runner | Repository CI baseline |
| Merge gate | Not required |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. Choose a validation mode.
   - Weekly schedule and pushes use the default smoke path.
   - Monthly schedule and manual full runs use the deeper mode.
3. Run `bash scripts/run-devcontainer-smoke.sh` for smoke validation or
   `bash scripts/run-devcontainer-smoke.sh --run-tests` for the deeper mode.
4. Let the shared script build the devcontainer, run lifecycle hooks, verify .NET, Git,
   and Docker access, and optionally run `dotnet test --solution ViajantesTurismo.slnx`
   inside the container before cleanup.
5. Upload `devcontainer-smoke-logs` when the workflow fails.

This workflow is intentionally supplemental rather than required. It is meant to catch
environment drift in the repository's containerized developer path without running on every
pull request or expanding the required merge gate for ordinary application changes.
That trade-off means a devcontainer regression can now reach `main` before the scheduled,
manual, or post-merge smoke run catches it.

Because the workflow now uses the same script contributors can run locally, failures are
more reproducible and devcontainer changes only need to update one smoke-validation path.
The weekly cadence keeps the low-cost baseline fresh, while the monthly full run checks that
the complete in-container test suite still works without paying that cost every week.
The current optimization stance is intentionally conservative: the workflow keeps a pinned
Dev Container CLI path, but broader startup work stays deferred unless devcontainer latency
becomes a demonstrated contributor pain point.

## Release prep workflow

A separate workflow (`.github/workflows/release-prep.yml`) prepares release artifacts without
publishing by default. It runs on relevant pull-request changes and manual dispatch.

### Release Prep

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/release-prep.yml` |
| Primary job name | `Release Dry Run` |
| Runner | Repository CI baseline |
| Merge gate | Not required |

**Dry-run steps:**

1. Checkout full history so the existing version calculation can inspect tags and commits.
2. Run `SharedKernel.Versioning.Tool calculate-release` from the CI versioning flow.
3. Run `SharedKernel.Versioning.Tool pack-sharedkernel` with the computed package version.
4. Run `SharedKernel.Versioning.Tool api-compatibility` to write the package compatibility report.
5. Run `SharedKernel.Versioning.Tool prepare-release` to generate `release-notes.md`, `CHANGELOG.md`,
   and `release-manifest.json`.
6. Upload package and release-prep artifacts for review.

The existing CI workflow remains the build/test gate for pull requests. Release Prep intentionally
stays focused on release artifact generation so it does not duplicate full-solution validation.

The stable path is manual-only. It requires the `release` environment approval and the
`promote_stable` dispatch input before it can create a `vX.Y.Z` tag. GitHub release creation and
NuGet publishing remain separate explicit dispatch inputs. NuGet publishing also requires the
`NUGET_API_KEY` secret scoped to the approved environment.

The current release-prep manifest records package file names, SHA-256 hashes, sizes, source SHA,
and version metadata. A formal SBOM is not generated yet because the repository does not currently
have an SBOM generator wired into local or CI tooling. Add SBOM generation as a dedicated follow-up
when a pinned repository-approved tool is selected.

### Aspire Release Integration Boundary

Release workflows should reuse the same `calculate-release` output. AppHost code stays the
orchestration model and receives precomputed values from the workflow; it must not inspect Git tags or
commit history itself.

For application container publishing, pass the computed release version into `aspire publish` and map
it to Aspire's container hooks documented in `src/ViajantesTurismo.AppHost/README.md`:

1. Convert selected project resources with `PublishAsDockerFile(...)` during publish mode.
2. Apply the computed `package_version` image tag with `WithImageTag(...)`.
3. Apply workflow-owned registry settings with `WithImageRegistry(...)` or push options when needed.
4. Use manifest callbacks only for metadata that is not already covered by Aspire resource annotations.

Keep infrastructure container tags and digest pins independent from app release tags.
