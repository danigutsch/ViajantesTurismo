# Supplemental workflows

This document covers the repository's governance and environment-parity workflows that run
outside the main `.github/workflows/ci.yml` validation path.

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
| Runner | `ubuntu-latest` |
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
| Runner | `ubuntu-latest` |
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
manual dispatch, and for pull requests or pushes that touch devcontainer and bootstrap inputs
such as `.devcontainer/**`, `.nvmrc`, `global.json`, or dependency manifests for npm and
NuGet packages.

### Devcontainer Smoke

| Attribute | Value |
| --- | --- |
| Workflow file | `.github/workflows/devcontainer-smoke.yml` |
| Primary job name | `Devcontainer Smoke` |
| Runner | `ubuntu-latest` |
| Merge gate | Not required |

**Steps:**

1. Checkout repository (`actions/checkout`).
2. Set up Node.js from `.nvmrc` (`actions/setup-node`).
3. Choose a validation mode.
    - Weekly schedule, pull requests, and pushes use the default smoke path.
    - Monthly schedule and manual full runs use the deeper mode.
4. Run `bash scripts/run-devcontainer-smoke.sh` for smoke validation or
   `bash scripts/run-devcontainer-smoke.sh --run-tests` for the deeper mode.
5. Let the shared script build the devcontainer, run lifecycle hooks, verify .NET, Node.js,
   Git, and Docker access, and optionally run `dotnet test --solution ViajantesTurismo.slnx`
   inside the container before cleanup.
6. Upload `devcontainer-smoke-logs` when the workflow fails.

This workflow is intentionally supplemental rather than required. It is meant to catch
environment drift in the repository's containerized developer path without expanding the
required pull-request gate for ordinary application changes.

Because the workflow now uses the same script contributors can run locally, failures are
more reproducible and devcontainer changes only need to update one smoke-validation path.
The weekly cadence keeps the low-cost baseline fresh, while the monthly full run checks that
the complete in-container test suite still works without paying that cost every week.
