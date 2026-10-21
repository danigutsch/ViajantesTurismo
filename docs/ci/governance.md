# Governance

This document describes the repository policies that govern GitHub Actions usage,
repository-owned CI settings, and required checks.

For the current CI dependency acquisition and downloaded-tool baseline, see
[`security-hardening.md`](security-hardening.md).

For workflow event trust boundaries, fork PR handling, and privileged follow-up job notes,
see [`trust-boundaries.md`](trust-boundaries.md).

## Branch protection rules

Branch protection and the active `main` ruleset require these status checks:

- `Build and Test` (from `.github/workflows/ci.yml`)
- `Lint` (from `.github/workflows/ci.yml`)
- `Dependency Review` (from `.github/workflows/dependency-review.yml`)
- `Secret Scan` (from `.github/workflows/secret-scan.yml`)
- `Quality Gate` (from `.github/workflows/ci.yml`)

The required status changed from `SonarCloud` to `Quality Gate` after the replacement check
passed. Updating both protections then allowed the replacement workflow to merge. The settings
were verified on October 20, 2026 in branch protection and ruleset `14460324`. Strict up-to-date
checks, GitHub Actions check attribution (app `15368`), the other required checks, signed commits,
additional review requirements, and the no-bypass policy were preserved.
See [quality gates](quality-gates.md#branch-protection) for the maintenance runbook.

`Build and Test` aggregates the test matrix and `OpenAPI Tool Windows`. `Quality Gate`
aggregates selected coverage and enforces changed-code coverage without secrets, including on
fork pull requests. Failed tests cannot be bypassed by successful coverage aggregation.

Required names must match workflow `name:` fields. `Devcontainer Smoke` remains supplemental
and is not part of the required merge gate.

## Recovery for missing PR checks

If a protected pull request shows required checks as "Expected" on the latest head SHA, verify
whether GitHub created any `pull_request` workflow runs for that exact SHA before assuming the
code or tests are at fault.

Repository recovery order:

1. Confirm the current PR head SHA.
2. Confirm whether `pull_request` runs exist for that SHA.
3. If none exist, trigger another PR activity event by editing the PR.
4. If editing does not create fresh PR-context runs, close and reopen the PR.

The required workflows in this repository listen to `pull_request.edited` specifically so a
small metadata edit can be used as the first recovery step. Manual `workflow_dispatch` runs may
be helpful for diagnostics, but they are not the preferred recovery path for satisfying branch
protection on the current PR head SHA.

## Signed commit policy

The repository policy is to require verified signed commits for merges to the protected
`main` branch.

- Any signature type that GitHub marks as **Verified** is acceptable for this policy.
- The documented contributor path for this repository remains **GPG commit signing** so
  setup and troubleshooting stay consistent.
- Verified commit signatures strengthen authorship and change provenance, but they do
  **not** replace code review, required status checks, or key lifecycle management.
- GitHub's verification record is persistent. A commit that was verified when pushed can
  remain marked verified later even if the key is rotated, revoked, or expires.

### Merge and automation implications

- **Squash and merge** is not permitted in this repository.
- **Rebase and merge** is not permitted for branches that require signed commits because
  GitHub cannot preserve commit signature verification on that merge path.
- **Create a merge commit** is the only permitted merge method. GitHub creates and signs
  the resulting merge commit, which is marked **Verified** on `main`.
- Dependabot and other bots are unaffected while they work on pull request branches, but
  any commit that ultimately lands on `main` must still satisfy the signed-commit rule.

### Enforcement note

A maintainer with repository admin access must keep GitHub branch protection or ruleset
settings aligned with this policy by enabling **Require signed commits** for `main`.

## Action versioning policy

All external GitHub Actions used in repository workflows are pinned to immutable commit
SHAs with the upstream release noted in an inline comment, for example
`actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6`.

### Rationale

Immutable SHA pinning is the repository baseline for workflow supply-chain hardening.

- GitHub executes the exact reviewed revision referenced by the workflow, not whatever tag
  is moved to later.
- Inline version comments preserve readability and make review of Dependabot action
  updates straightforward.
- The workflow surface stays narrow by following the allowed dependency acquisition patterns in
  [Security hardening baseline](security-hardening.md#allowed-dependency-acquisition-patterns).

### Trusted actions

The workflow uses a small reviewed action set.

| Action | Purpose |
| --- | --- |
| `actions/checkout` | Repository checkout |
| `actions/setup-dotnet` | .NET SDK provisioning |
| `actions/cache` | Reviewed dependency caching where configured |
| `actions/download-artifact` | Retrieve SARIF artifact for the dedicated code-scanning upload job |
| `actions/upload-artifact` | Test result artifact upload |
| `actions/dependency-review-action` | PR dependency and license review |
| `github/codeql-action/upload-sarif` | Upload SARIF results from secret scanning |

Coverage collection uses `coverlet.MTP` 10.1.0 and the pinned local
`dotnet-reportgenerator-globaltool` 5.5.11 for HTML and merged Cobertura. The C# repository tool
owns changed-code threshold validation. No scanner or proprietary coverage local tool is needed.
The workflow lint path installs pinned `actionlint` 1.7.9 directly.

### Update process

Use the [pin update review workflow](pin-update-review.md) for coverage gaps, required SHA/digest
evidence, compatibility checks, and stateful recovery.

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The
  configuration covers `github-actions`, `dotnet-sdk`, `nuget`, and `devcontainers`
  ecosystems with deliberately cautious schedules for routine version updates.
- When Dependabot proposes an action update, review both the release notes and the
  resolved SHA, then verify the affected workflows still pass before merging.
- When upgrading across major action versions, review the migration guidance before
  accepting the new SHA-pinned reference.

## Workflow ownership

The `CODEOWNERS` file at the repository root requires review for all changes to workflow
files.

Any pull request that modifies `.github/workflows/**` will request review from the
designated code owners. See `CODEOWNERS` for the current ownership mapping.

## Dependabot configuration

`.github/dependabot.yml` automates version update PRs for four ecosystems.

| Ecosystem | Scope | Schedule | PR limit | Update shaping |
| --- | --- | --- | --- | --- |
| `github-actions` | Workflow action references | Monthly at 05:00 UTC | 1 | All action updates grouped into one PR |
| `dotnet-sdk` | Repository SDK pin | Monthly at 05:00 UTC | 1 | Minor and patch updates grouped, longer cooldown enabled |
| `nuget` | .NET package dependencies | Monthly at 05:00 UTC | 1 | Minor and patch updates grouped, security updates grouped, longer cooldown enabled |
| `devcontainers` | Dev Container Features in valid `devcontainer.json` locations | Quarterly at 05:00 UTC | 1 | All feature updates grouped into one PR |

Dependabot PRs use conventional commit prefixes (`ci` for actions, `deps` for packages).

The repository uses explicit custom labels for ecosystems where it wants a different triage
taxonomy than the default ecosystem label. The `devcontainers` entry also relies on defaults
so the repository does not need to maintain a separate custom label for that ecosystem.
Those custom labels are managed in `.github/labels.json` and synced by
`.github/workflows/sync-labels.yml`.

No separate `docker` ecosystem entry is configured because the repository does not
currently contain Dockerfiles, Docker Compose files, or Kubernetes manifests for
Dependabot's Docker updater to monitor. It also cannot update image references declared
in C# resource code, which require a separate manual review path.

The configuration intentionally reduces PR churn instead of accepting Dependabot's
default one-PR-per-update behavior.

- `open-pull-requests-limit` keeps concurrent version-update PR volume bounded per
  ecosystem.
- `groups` consolidate related updates so low-risk churn does not fan out into many
  small PRs.
- `cooldown` delays fast-follow NuGet version updates, with deliberately longer
  waits than before so newly published releases have more time to prove they are stable
  and trustworthy before the bot proposes them here.
- monthly schedules keep ordinary version drift reviewable instead of constantly landing
  fresh upgrade PRs in the queue.

Security updates remain intentionally prompt. Cooldown only affects version updates, not
security updates, so vulnerable dependencies can still surface quickly when GitHub
identifies an advisory.
