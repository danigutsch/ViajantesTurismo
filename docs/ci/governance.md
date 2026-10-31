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
- `PR Metadata` (from `.github/workflows/pr-metadata.yml`)

`Dependency Review` and `PR Metadata` call shared reusable workflows, so GitHub reports them as
`Dependency Review / Review` and `PR Metadata / Validate squash header`. The list above names the workflow
jobs; the ruleset must require the reported names. Updating the ruleset is a manual step to do before the
change that adopts the shared workflows merges. See [SharedKernel consumption](../sharedkernel/development.md#shared-ci-references-and-the-pin-bump).

Branch protection and ruleset `14460324` require these checks with strict up-to-date checks,
GitHub Actions check attribution (app `15368`), signed commits, additional review requirements,
and no bypass.
See [quality gates](quality-gates.md#branch-protection) for the maintenance runbook.

`Build and Test` aggregates the test matrix. `Quality Gate`
checks the selected slices' coverage and enforces changed-code coverage without secrets, including on
fork pull requests. Failed tests cannot be bypassed by successful coverage aggregation.

Required names must match workflow `name:` fields. `Devcontainer Smoke` remains supplemental
and is not part of the required merge gate.

## Recovery for missing PR checks

If a protected pull request shows required checks as "Expected" on the latest head SHA, verify
whether GitHub created any `pull_request` workflow runs for that exact SHA before assuming the
code or tests are at fault.

Repository recovery order:

1. Confirm the current PR head SHA and whether PR-context runs exist for it.
2. Add the `ci:rerun` label to explicitly start fresh source validation and metadata checks.
3. For another attempt, remove and reapply the label using a human account or an authorized app.
4. If label delivery fails, close and reopen the PR.

Ordinary title/body edits run only `PR Metadata`. They do not restart or cancel source-validation
runs. Unrelated label events use separate concurrency groups and ignored check names; skipped
jobs cannot overwrite the results of required source checks. Workflows do not grant themselves
label-write permissions. A label applied by `GITHUB_TOKEN` may not trigger another workflow;
use the existing human/app authentication path.

Manual `workflow_dispatch` remains diagnostic rather than a substitute for missing PR-context
checks. See [CI throughput](throughput.md) for the rollout and measurement procedure.

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

- **Squash and merge** is the only permitted merge method after the settings rollout. Use the PR
  title as the commit header and the PR body as its description. Metadata validation checks the
  Conventional Commit header independently of source checks.
- GitHub signs the resulting squash commit, but required signing can still reject unsigned PR commits.
- Disable **Create a merge commit** and **Rebase and merge**. Preserve signed commits, strict
  up-to-date checks, review settings, and existing required checks.
- Dependabot and other bots are unaffected while they work on pull request branches, but
  any commit that ultimately lands on `main` must still satisfy the signed-commit rule.

### Enforcement note

A maintainer with repository admin access must keep GitHub branch protection or ruleset
settings aligned with this policy by enabling **Require signed commits** for `main`.

## Action versioning policy

All external GitHub Actions used in repository workflows are pinned to immutable commit
SHAs with the upstream release noted in an inline comment, for example
`actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2`.

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
The workflow lint path runs SharedKernel's reusable actionlint workflow, which installs its default pinned
`actionlint` release (currently 1.7.9) and verifies the archive checksum.

### Update process

Use the [pin update review workflow](pin-update-review.md) for coverage gaps, required SHA/digest
evidence, compatibility checks, and stateful recovery.

- GitHub Dependabot automates version update PRs via `.github/dependabot.yml`. The
  configuration covers `github-actions`, `nuget`, and `devcontainers` ecosystems with
  deliberately cautious schedules for routine version updates. SDK and SharedKernel action
  updates arrive only with a SharedKernel pin bump.
- Third-party actions listed in SharedKernel's `.github/action-pins.json` are ignored by Dependabot: `verify-action-pins` in the Lint job fails on any
  drift from that manifest, so a bump starts from the manifest and then updates every reference here.
- When Dependabot proposes an action update, review both the release notes and the
  resolved SHA, then verify the affected workflows still pass before merging.
- When upgrading across major action versions, review the migration guidance before
  accepting the new SHA-pinned reference.

## Workflow ownership

The `CODEOWNERS` file at the repository root requires review for all changes to workflow
files, composite actions, and repository configuration.

Any pull request that modifies `.github/workflows/**`, `.github/actions/**`, or `.config/**`
(the SharedKernel pin, local tool manifest, and license exceptions) will request review from the
designated code owners. See `CODEOWNERS` for the current ownership mapping.

## Dependabot configuration

`.github/dependabot.yml` automates version update PRs for four ecosystems.

| Ecosystem | Scope | Schedule | PR limit | Update shaping |
| --- | --- | --- | --- | --- |
| `github-actions` | Workflow action references | Monthly at 05:00 UTC | 1 | `danigutsch/PandaBear.SharedKernel` and every manifest-pinned action ignored; bumps start from the SharedKernel manifest |
| `dotnet-sdk` | Repository SDK pin | Monthly at 05:00 UTC | Not applicable | All updates ignored |
| `nuget` | .NET package dependencies | Monthly at 05:00 UTC | 1 | Minor and patch updates grouped, security updates grouped, longer cooldown enabled |
| `devcontainers` | Dev Container Features in valid `devcontainer.json` locations | Quarterly at 05:00 UTC | 1 | All feature updates grouped into one PR |

Dependabot PRs use conventional commit prefixes (`ci` for actions, `deps` for packages).

Two update streams move only with a SharedKernel pin bump, so Dependabot ignores them:

- `global.json` must match the SharedKernel `global.json` at the pinned `libraryRevision` (same
  `sdk.version` and `rollForward`). The SharedKernel tool build and `stage-candidate` fail when
  they differ.
- `danigutsch/PandaBear.SharedKernel` references have two roots. The candidate and scanner action
  (`setup-security-scanner` in `secret-scan.yml`) equal the pinned `libraryRevision`. The reusable workflows,
  `setup-dotnet-tool`, and the `source-ref` and `revision` inputs use one shared-workflow SHA that moves together
  with `libraryRevision` in the pin bump. `SharedKernelConsumerWorkflowTests` enforces both.

See the [pin update review workflow](pin-update-review.md) for the pin bump steps.

The repository uses explicit custom labels for ecosystems where it wants a different triage
taxonomy than the default ecosystem label. The `devcontainers` entry also relies on defaults
so the repository does not need to maintain a separate custom label for that ecosystem.
Those custom labels are the `labels` section of `.config/sharedkernel-repo.json`;
`.github/workflows/sync-labels.yml` creates the missing ones and updates those that differ with
`sharedkernel-repo github sync-labels --force`, and never deletes a label.

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
