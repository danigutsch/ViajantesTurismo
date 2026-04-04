# Contributing

## Workflow

1. Install local tooling with `./setup-dev.ps1` on Windows or `bash ./setup-dev.sh` on Unix.
2. Read the nearest applicable `AGENTS.md` file before making changes; repository customization guidance lives in the `AGENTS.md` hierarchy.
3. Do not add duplicate repository guidance files (for example, replacement `.github/copilot-instructions.md` or ad hoc `.github/instructions/*.instructions.md`
 files) unless there is a clear scoped need that the existing `AGENTS.md` hierarchy cannot express.
4. Make focused changes and keep commits small enough to describe clearly.
5. Run the relevant checks before opening a pull request.
6. Open a pull request using the repository template and complete the checklist.

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

## Signed Commits

Merges to the protected `main` branch are required to use **verified signed commits**.
The only permitted merge method is **Create a merge commit**; squash and rebase are not
allowed. GitHub creates and signs the merge commit, marking it **Verified** on `main`.
Any signature type that GitHub marks as **Verified** is acceptable, but this repository
documents **GPG signing** as the recommended contributor path.

### Recommended one-time GPG setup

1. Ensure you already have a GPG key and that the public key is added to your GitHub
   account.
2. Configure Git to use your signing key:

   ```text
   git config --global user.signingkey <your-gpg-key-id>
   git config --global commit.gpgsign true
   git config --global tag.gpgsign true
   ```

3. Create a signed commit and confirm GitHub shows the commit as **Verified**.

### Troubleshooting verification failures

- If GitHub shows **Unverified**, check that the commit email matches an email address
  associated with your GitHub account and that the public GPG key is uploaded to the
  same account.
- If you created an unsigned commit by mistake, rewrite it with a signature before
  merge, for example by amending or rebasing with signing enabled.
- GitHub records verification when the signed commit is pushed. A previously verified
  commit can remain marked verified later even if the key is rotated, revoked, or
  expires.
- For branch-protection behavior and merge-method caveats, see
  [docs/ci/governance.md](docs/ci/governance.md).

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
