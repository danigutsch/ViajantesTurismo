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
| `ci.yml` SonarCloud | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Same-repo branches trusted; fork PRs untrusted | Uses `SONAR_TOKEN` and `GITHUB_TOKEN`; fork PRs must skip secret-dependent steps | This is the main secret-bearing CI boundary. |
| `dependency-review.yml` | `pull_request`, `merge_group` | PR code not executed as shell logic | Read-only `contents` | Uses GitHub's dependency review action on manifest diffs. |
| `actionlint.yml` | PRs touching workflow files, `push main`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no secrets | Downloads pinned tooling but does not mutate repo state. |
| `secret-scan.yml` scan job | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Fork PRs untrusted; same-repo branches trusted | Read-only `contents`; no write scopes | Produces SARIF artifact in a low-privilege job. |
| `secret-scan.yml` SARIF upload job | `pull_request`, `merge_group`, `push`, `workflow_dispatch` | Only safe for repo-owned refs or same-repo PRs | `security-events: write` only in the follow-up job | Explicit artifact handoff keeps write scope out of the scanner job. |
| `devcontainer-smoke.yml` | `push main`, `schedule`, `workflow_dispatch` | Repository-owned only | Read-only `contents`; no secrets | Not exposed to fork PR code. |
| `sync-labels.yml` | `push main`, `workflow_dispatch` | Repository-owned only | `issues: write` | Administrative automation only; never runs on pull requests. |

## Current conclusions

- The repository already avoids `pull_request_target` for build, test, and analysis of
  untrusted pull request code.
- The repository already keeps the only current GitHub write scope used in scan workflows
  (`security-events: write`) in a dedicated follow-up job rather than the scanning job
  itself.
- Repository-mutation automation (`sync-labels.yml`) is limited to `push` on `main` and
  manual dispatch, which keeps fork pull requests out of that write path.
- The main remaining trust-boundary clarification needed in CI is the SonarCloud path:
  fork pull requests do not receive `SONAR_TOKEN`, so the workflow should skip
  secret-dependent analysis explicitly instead of failing during configuration validation.

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
