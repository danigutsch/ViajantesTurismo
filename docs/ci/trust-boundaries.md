# CI trust boundaries

This document records the repository's current GitHub Actions trust model for pull
requests, merge queue validation, and repository-owned automation.

## Boundary summary

The repository currently has three practical trust zones:

- Untrusted fork pull request code.
- Trusted same-repository branches, including maintainer and bot pull requests.
- Repository-owned post-merge or manually dispatched automation.

The key rule is that untrusted fork pull request code must not receive repository secrets or
write-scoped GitHub token permissions.

## Workflow trust map

| Workflow | Trigger classes | Code trust level | Token/secrets posture | Notes |
| --- | --- | --- | --- | --- |
| `ci.yml` validation slices | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`, plus `pull-requests: read` where needed; no write scopes in test jobs | Build/test jobs can execute PR code but should stay read-only. |
| `ci.yml` Quality Gate | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no analysis secrets | Coverage validation runs for forks too; checkout does not persist credentials. |
| `dependency-review.yml` | `pull_request`, `merge_group`, `push main`, `workflow_dispatch` | Snapshots run for same-repository non-Dependabot PRs and repository-owned events; forks and Dependabot retain read-only review | `contents: write` only for the guarded snapshot job; `GH_TOKEN` only on its `dependency submit --force` step; review stays read-only | Builds `SharedKernel.RepoConfig.Tool` from the pinned SharedKernel source, reads committed lock files for exact base/head SHAs without restoring them, submits the snapshots, then reviews the graph; see dependency/license evidence. |
| `pr-metadata.yml` | `pull_request` (including title and body edits) | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no secrets or token | Builds `SharedKernel.CI.Tool` from the pinned SharedKernel source; PR text reaches it only through `--event "$GITHUB_EVENT_PATH"`. |
| `actionlint.yml` | PRs touching workflow files, `push main`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no secrets | Downloads pinned tooling but does not mutate repo state. |
| `secret-scan.yml` scan job | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no write scopes | Produces SARIF artifact in a low-privilege job. |
| `secret-scan.yml` SARIF upload job | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Only safe for repo-owned refs or same-repo PRs | `security-events: write` only in the follow-up job | Explicit artifact handoff keeps write scope out of the scanner job. |
| `devcontainer-smoke.yml` | `push main`, `schedule`, `workflow_dispatch` | Repository-owned only | Read-only `contents`; no secrets | Not exposed to fork PR code. |
| `sync-labels.yml` | `push main`, `workflow_dispatch` | Repository-owned only | `issues: write` | Administrative automation only; never runs on pull requests. Checkout keeps no credentials, and only the `sharedkernel-repo github sync-labels --force` step receives `GH_TOKEN`. |

## Current conclusions

- The repository already avoids `pull_request_target` for build, test, and analysis of
  untrusted pull request code.
- The repository already keeps the only current GitHub write scope used in scan workflows
  (`security-events: write`) in a dedicated follow-up job rather than the scanning job
  itself.
- Repository-mutation automation (`sync-labels.yml`) is limited to `push` on `main` and
  manual dispatch, which keeps fork pull requests out of that write path.
- Quality aggregation sends no source-analysis data to an external service and needs no analysis
  token. Coverage artifacts remain untrusted inputs and are validated by the repository tool.

## SharedKernel source and candidate boundary

`.config/sharedkernel-candidate.json` is code-owned and pins the SharedKernel `libraryRevision`,
package version, and package SHA-256 hashes.

- `.github/actions/build-sharedkernel-tool` requires a 40-character lowercase hexadecimal
  `libraryRevision`, checks out only `tools/<tool>` and `tools/Shared` from
  `danigutsch/PandaBear.SharedKernel` at that commit with `persist-credentials: false` and no token,
  and builds the tool through SharedKernel's `setup-dotnet-tool` in locked restore mode. It accepts only
  `SharedKernel.CI.Tool` and `SharedKernel.RepoConfig.Tool`. The optional `revision` input replaces
  `libraryRevision` with another full SHA (the Lint action pin check uses the shared-workflow SHA), and the
  optional `sparse-extra` input adds one more directory, such as `.github`, to the sparse checkout.
- `sharedkernel-ci stage-candidate` requires the release `candidate.json` identity to equal the pin,
  downloads release assets over HTTPS without a token, and verifies every package hash before it
  enters `.local/sharedkernel-feed`. Package source mapping restores `PandaBear.SharedKernel.*` only
  from that feed.
- `danigutsch/PandaBear.SharedKernel` action references have two roots: the scanner action equals the pinned
  `libraryRevision`; reusable workflows, `setup-dotnet-tool`, `source-ref` and `revision` use one shared-workflow SHA
  that moves with it in the pin bump.
- A pull request that changes the pin, the composite actions, or `.config/**` requires code-owner
  review; see [governance](governance.md#workflow-ownership).

## Recommended baseline

- Keep build/test workflows on `pull_request`, not `pull_request_target`.
- Keep privileged follow-up jobs separate from unprivileged artifact-producing jobs.
- Keep write-scoped automation restricted to repository-owned events such as `push` to
  `main`, merge queue, or manual dispatch.
- Make fork PR skips explicit anywhere a job depends on secrets or write-scoped token
  permissions.
- Treat same-repository pull requests as the collaborator trust boundary where secrets may be
  acceptable, and document that assumption clearly.

## Follow-up direction

- The CI supply-chain baseline for tool acquisition and pinning is now documented in
  [security-hardening.md](security-hardening.md).
- Local lint and helper-tool execution still needs a decision on whether it must match the same
  trust restrictions and documentation level as hosted CI.
- Any privileged follow-up job hardening should build on both the accepted CI supply-chain
  baseline and this trust-boundary guidance.
