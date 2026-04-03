# Contributing

## Workflow

1. Install local tooling with `./setup-dev.ps1` on Windows or `bash ./setup-dev.sh` on Unix.
2. Make focused changes and keep commits small enough to describe clearly.
3. Run the relevant checks before opening a pull request.
4. Open a pull request using the repository template and complete the checklist.

## Commit Messages

This repository uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
Commit messages must follow this format:

```text
<type>[optional scope]: <description>
```

- `type` must be one of: `feat`, `fix`, `docs`, `ci`, `build`, `test`, `refactor`, `perf`, `style`, `chore`, `revert`
- `scope` is optional and should identify the affected area, such as `web`, `domain`, `tests`, or `ci`
- `description` should be short, lowercase where practical, and describe the change in imperative form

Examples:

```text
ci: add dependency review workflow
docs(ci): document required branch protection checks
fix(web): handle missing booking date in admin form
```

Breaking changes should use `!` or a `BREAKING CHANGE:` footer:

```text
feat(api)!: remove legacy booking endpoint
```

Git hooks install a `commit-msg` check that validates messages with `commitlint`.
If you need to bypass hooks for an emergency, use `git commit --no-verify` and fix the history before merge.

## Pull Requests

- Use the pull request template
- Summarize the user-visible change and the technical approach
- List the checks you ran locally
- Link related backlog items, issues, or ADRs when applicable
- Update documentation when behavior, workflow, or contributor expectations change

## Quality Checks

Run the checks relevant to your changes:

- `.NET`: `dotnet build ViajantesTurismo.slnx`
- Tests: `dotnet test --solution ViajantesTurismo.slnx`
- Docs, scripts, specs: `npm run lint:all`

If you change NuGet dependencies or project references that affect package
resolution, regenerate and commit the affected `packages.lock.json` files:

- Refresh lock files: `dotnet restore ViajantesTurismo.slnx --force-evaluate`
- Verify locked restore: `dotnet restore ViajantesTurismo.slnx --locked-mode`

See `docs/CODE_QUALITY.md` for the full local tooling reference.
