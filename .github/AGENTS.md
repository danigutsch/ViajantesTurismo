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
- Keep workflow `run` sections concise and delegate complex logic to scripts.

## Hooks conventions

- Keep deterministic hook commands and explicit timeouts.
- Avoid destructive behavior in hooks.
- Prefer warn/block policies that are documented in-repo.

## Prompt and customization assets

- Keep prompt files task-specific and reusable.
- Keep customization content concise, deterministic, and repository-relevant.

## References

- `AGENTS.md` (repository root)
- `.github/copilot-instructions.md`
