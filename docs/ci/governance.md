# Governance

This document describes the repository policies that govern GitHub Actions usage,
repository-owned CI settings, and required checks.

## Branch protection rules

Branch protection for `main` is configured to require the following status checks:

- `Build and Test` (from `.github/workflows/ci.yml`)
- `Lint` (from `.github/workflows/ci.yml`)
- `Dependency Review` (from `.github/workflows/dependency-review.yml`)
- `Secret Scan` (from `.github/workflows/secret-scan.yml`)
- `SonarCloud` (from `.github/workflows/ci.yml`)

These names match the `name:` fields in the respective workflow files. Any rename of the
jobs must be reflected in branch protection settings.

Representative pull request validation has also been observed successfully with these
checks, including the main CI workflow and its integrated SonarCloud status job, the
separate dependency review workflow, and the separate secret scanning workflow. The
`Devcontainer Smoke` workflow remains supplemental and is not part of the required merge
gate.

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

- GitHub's **Rebase and merge** path should not be used for protected branches that
  require signed commits, because GitHub cannot preserve commit signature verification on
  that merge path.
- GitHub's **Squash and merge** path is generally compatible with branches that require
  signed commits because GitHub creates the resulting squash commit and marks it
  **Verified** when that merge path is allowed. Maintainers should validate repository
  settings and observed GitHub behavior if this policy or GitHub enforcement changes.
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
- The workflow surface still stays narrow because the repository uses only official
  GitHub-maintained actions.

### Trusted actions

The workflow uses only official GitHub-maintained actions.

| Action | Purpose |
| --- | --- |
| `actions/checkout` | Repository checkout |
| `actions/setup-dotnet` | .NET SDK provisioning |
| `actions/setup-node` | Node.js provisioning |
| `actions/cache` | SonarCloud package cache |
| `actions/download-artifact` | Retrieve SARIF artifact for the dedicated code-scanning upload job |
| `actions/upload-artifact` | Test result artifact upload |
| `actions/dependency-review-action` | PR dependency and license review |
| `github/codeql-action/upload-sarif` | Upload SARIF results from secret scanning |

SonarCloud integration is implemented through repo-pinned local .NET tools rather than an
additional third-party GitHub Action. The workflow lint path also installs the pinned
`actionlint` `1.7.9` release binary directly rather than adding another GitHub Action
dependency.

The current hosted analysis path actively uses:

- `dotnet-sonarscanner` `11.2.0`
- `dotnet-reportgenerator-globaltool` `5.5.1`

The local tool manifest also contains `dotnet-coverage` `18.5.2` for supported .NET
coverage workflows, but the current SonarCloud workflow does not rely on the direct
`dotnet-coverage -f xml` path.

This keeps the GitHub Actions dependency surface narrow while still adopting the
SonarCloud analysis model used in BookWorm's quality strategy.

### Update process

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The
  configuration covers `github-actions`, `devcontainers`, `nuget`, and `npm`
  ecosystems on a deliberately cautious monthly schedule for routine version updates.
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
| `devcontainers` | Dev Container Features in valid `devcontainer.json` locations | Monthly at 05:00 UTC | 1 | All feature updates grouped into one PR |
| `nuget` | .NET package dependencies | Monthly at 05:00 UTC | 1 | Minor and patch updates grouped, security updates grouped, longer cooldown enabled |
| `npm` | Node.js dependencies | Monthly at 05:00 UTC | 1 | Minor and patch updates grouped, security updates grouped, longer cooldown enabled |

Dependabot PRs use conventional commit prefixes (`ci` for actions, `deps` for packages).

The repository relies on Dependabot defaults for npm labels and uses explicit custom
labels for ecosystems where the repository wants a different triage taxonomy than the
default ecosystem label. The `devcontainers` entry also relies on Dependabot defaults
so the repository does not need to maintain a separate custom label for that ecosystem.
Those custom labels are managed in `.github/labels.json` and synced by
`.github/workflows/sync-labels.yml`.

No separate `docker` ecosystem entry is configured because the repository does not
currently contain Dockerfiles, Docker Compose files, or Kubernetes manifests for
Dependabot's Docker updater to monitor.

The configuration intentionally reduces PR churn instead of accepting Dependabot's
default one-PR-per-update behavior.

- `open-pull-requests-limit` keeps concurrent version-update PR volume bounded per
  ecosystem.
- `groups` consolidate related updates so low-risk churn does not fan out into many
  small PRs.
- `cooldown` delays fast-follow NuGet and npm version updates, with deliberately longer
  waits than before so newly published releases have more time to prove they are stable
  and trustworthy before the bot proposes them here.
- monthly schedules keep ordinary version drift reviewable instead of constantly landing
  fresh upgrade PRs in the queue.

Security updates remain intentionally prompt. Cooldown only affects version updates, not
security updates, so vulnerable dependencies can still surface quickly when GitHub
identifies an advisory.
