# AGENTS.md

Instructions for files under `.github/`.

## Scope and precedence

- Applies to all files under `.github/`.
- If instructions conflict with repository root guidance, follow this file for `.github` changes.

## Workflow conventions

- Pin external actions to full commit SHAs.
- Use least-privilege job permissions.
- Prefer explicit concurrency settings for branch/PR workflows.
- Keep secrets in `${{ secrets.* }}` and non-sensitive config in `${{ vars.* }}`.
- Keep workflow `run` sections concise and delegate complex logic to SharedKernel tool commands.
- Run SharedKernel tools with `dotnet tool run <command> --` after `setup-ci-prerequisites` stages the pinned
  candidate. Jobs that run before staging use `./.github/actions/build-sharedkernel-tool` and
  `dotnet run --project "$SHAREDKERNEL_SOURCE/tools/<tool>" -c Release --no-build -- <command>`.
- Set `persist-credentials: false` on every checkout. Pass values to commands through `env:`, pass pull request text
  only through `--event "$GITHUB_EVENT_PATH"`, and set `GH_TOKEN` only on steps that call the GitHub API.
- Pin `danigutsch/PandaBear.SharedKernel` action references to the `libraryRevision` in
  `.config/sharedkernel-candidate.json`.
- Keep each required check's job `name:` expression and `ci:rerun` ignored-label `if:` pattern; see
  `docs/ci/governance.md`.
- `SharedKernelToolCallerTests` parses every SharedKernel tool command line; keep new command lines parseable without
  shell logic beyond the variables and arrays it maps.

## Hooks conventions

- Keep deterministic hook commands and explicit timeouts.
- Avoid destructive behavior in hooks.
- Prefer warn/block policies that are documented in-repo.

## Prompt and customization assets

- Keep prompt files task-specific and reusable.
- Keep customization content concise, deterministic, and repository-relevant.
- Prefer the repository `AGENTS.md` hierarchy for durable repository guidance.
- Do not reintroduce duplicate `.github/copilot-instructions.md` or broad `.github/instructions/*.instructions.md` files unless a genuinely narrower scoped
 rule is required.

## References

- `AGENTS.md` (repository root)
