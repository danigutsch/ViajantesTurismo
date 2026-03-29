# CI overview

This folder contains maintainer-facing documentation for the repository's GitHub Actions
validation and governance setup.

## Current model

The repository uses a consolidated main validation workflow in
`.github/workflows/ci.yml` plus separate governance workflows for dependency review,
workflow linting, secret scanning, and supplemental devcontainer validation.

The main CI workflow runs on:

- pull requests targeting `main`
- pushes to `main`
- merge queue checks (`merge_group`)
- `workflow_dispatch`

The workflow-level concurrency policy cancels stale runs for non-`main` refs while
preserving in-flight `main` runs. This keeps pull request iteration responsive without
interrupting post-merge validation on the protected branch.

Dependabot version updates now cover GitHub Actions, Dev Container Features, NuGet, and
npm dependencies. See [Governance](governance.md#dependabot-configuration) for the
current schedules and grouping rules.

## Documentation map

- [Main workflow](main-workflow.md) — triggers, jobs, docs-only optimization, and
  workflow evolution
- [SonarCloud](sonarcloud.md) — hosted analysis path, exclusions, and quality-gate
  expectations
- [Artifacts and local reproduction](artifacts-and-local-reproduction.md) — CI artifacts
  and the local commands used to reproduce failures
- [Governance](governance.md) — action pinning, CODEOWNERS, Dependabot, and branch
  protection
- [Supplemental workflows](supplemental-workflows.md) — dependency review, Actionlint,
  secret scan, and devcontainer smoke

## Required status checks

The required merge-gate checks for `main` are:

- `Build and Test`
- `Lint`
- `Dependency Review`
- `Secret Scan`
- `SonarCloud`

See [Governance](governance.md#branch-protection-rules) for the exact mapping to workflow
jobs.

## Next required work

The near-term required governance work of consolidating duplicated validation and
SonarCloud execution is complete. The next step is to monitor the merged workflow set in
normal use and adjust only when there is a concrete operational reason.

## Planned follow-up work

The current follow-up themes are:

- review which SonarCloud settings should remain in-repo versus move to the SonarCloud UI
  when plan capabilities allow it
- document the contributor devcontainer and Codespaces path more explicitly now that
  dependency update coverage includes Dev Container Features
- keep supplemental, non-required workflows under review if merge queue becomes part of
  the normal merge path
- consider multi-OS expansion only when a concrete cross-platform requirement appears

## Related documentation

- [README — Continuous Integration](../../README.md#continuous-integration) —
  contributor-facing CI summary
- [Code Quality Tools](../CODE_QUALITY.md) — local linting and formatting tools
- [Original CI delivery PBI](../backlog/PBI-2026-03-15-02-github-actions-ci-workflow.md)
  — baseline rollout record
- [CI governance follow-up PBI](../backlog/PBI-2026-03-29-03-ci-governance-follow-up-improvements.md)
  — planned improvements after the baseline rollout
