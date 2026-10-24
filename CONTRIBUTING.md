# Contributing

## Workflow

1. Install local tooling with `./setup-dev.ps1` on Windows or `bash ./setup-dev.sh` on Unix.
   Treat `README.md` as the canonical tooling inventory for required local tools,
   optional local tools, CI-only tools, and devcontainer-provided tools.
2. Agents should always work from repository-local Git worktrees under `.worktrees/`; that directory is ignored and only meant for local workspace management.
3. Read the nearest applicable `AGENTS.md` file before making changes; repository customization guidance lives in the `AGENTS.md` hierarchy.
4. Do not add duplicate repository guidance files (for example, replacement `.github/copilot-instructions.md` or ad hoc `.github/instructions/*.instructions.md`
 files) unless there is a clear scoped need that the existing `AGENTS.md` hierarchy cannot express.
5. Make focused changes and keep commits small enough to describe clearly.
6. Run the relevant checks before opening a pull request.
7. Open a pull request using the repository template and complete the checklist.

Repository agent skills live in `.agents/skills/<name>/SKILL.md`. `.claude/skills/<name>` is a relative symlink to the
same folder so Claude Code finds it; edit the `.agents` copy. On Windows, enable Developer Mode and run
`git config --global core.symlinks true` before cloning, otherwise Git checks the link out as a plain text file.

Custom Git hooks are not installed or maintained by this repository. Run local checks explicitly;
CI enforces the required quality gates.

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

Commit messages can be checked locally with
`dotnet tool run sharedkernel-ci -- verify-commit-message --file <path-to-commit-message-file>`.
To check each local commit, save this hook as the file that `git rev-parse --git-path hooks/commit-msg`
prints and make it executable. That path follows `core.hooksPath`; when only the global Git
configuration sets it, first set `core.hooksPath` in this repository to a directory of its own.

```sh
#!/bin/sh
exec dotnet tool run sharedkernel-ci -- verify-commit-message --file "$1"
```

## Signed Commits

Merges to the protected `main` branch are required to use **verified signed commits**.
Use **Squash and merge** with a Conventional Commit PR title of at most 100 characters.
GitHub signs the resulting squash commit. Keep individual PR commits signed: required-signature
rules can still reject unsigned commits before squash. Preserve breaking-change notes in the PR
body because the squash message uses that body. Start a fresh branch after merging.
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

- Use the pull request template in `docs/pull_request_template.md`
- Summarize the user-visible change and the technical approach
- List the checks you ran locally
- Link related backlog items, issues, or ADRs when applicable
- Update documentation when behavior, workflow, or contributor expectations change

### Updating an open pull request after its base merges

Prefer GitHub's **Update with rebase** action when it is available. It updates the existing remote
pull request branch without a local `git push --force-with-lease`.

After GitHub updates the branch, rebase or cherry-pick any local follow-up commits onto the updated
remote branch, then use a normal push. Do not push those commits onto the stale local branch.

If **Update with rebase** is unavailable and a non-linear pull request branch is acceptable, use a
signed merge from `main` followed by a normal push. Otherwise, open a follow-up pull request or close
and recreate the pull request.

## Quality Checks

Run the checks relevant to your changes:

- `.NET`: `dotnet build ViajantesTurismo.slnx`
- Tests: `dotnet test --solution ViajantesTurismo.slnx`
- Docs, scripts, specs: CI runs `bash scripts/lint-all.sh`
- Documentation links: `bash scripts/lint-links.sh` for focused local relative link, anchor, and
  durable GitHub issue/PR reference policy checks

Use the tooling inventory in `README.md` when deciding whether a missing tool is
required locally, optional for a specific task, CI-only, or already provided by
the documented devcontainer workflow.

## Dependency graph and lock-file maintenance

After intentionally changing any NuGet package or project reference, `Directory.Packages.props`,
restore-affecting build configuration, `NuGet.Config`, solution/project membership, `global.json`,
or `.config/dotnet-tools.json`, refresh the lock files:

```bash
dotnet tool restore
dotnet restore ViajantesTurismo.slnx --force-evaluate
dotnet restore ViajantesTurismo.slnx --locked-mode
```

These commands restore pinned local tools, regenerate `packages.lock.json`, and verify the result.

Review every generated lock-file change, including resolved-version changes, before committing it
with the dependency input that caused it. The refresh does not edit declared package or tool versions
and never auto-commits changes. Package-version inputs remain review-driven through Dependabot or an
intentional dependency update.

When resolving a rebase or merge conflict that affects dependency inputs or lock files:

1. Resolve the authoritative dependency inputs first.
2. Do not hand-merge or delete `packages.lock.json` files.
3. Run the lock-file refresh commands above.
4. Review the regenerated lock-file diff, then run the build and relevant test slice.

GitHub Action SHA pins and container image tag/digest pairs have their own review paths; do not run
the lock-file helper solely for those changes. See the [pin update review workflow](docs/ci/pin-update-review.md)
for Action SHA and image digest evidence. SDK updates use the same lock-file
refresh; do not create a separate SDK lock-refresh path.

### Updating the .NET SDK

The repository pins the exact build SDK in `global.json` with `"rollForward": "disable"`.
Contributors and CI must install that SDK before restore so a later installed SDK cannot change
SDK-injected dependencies. Apply SDK servicing updates by changing `global.json` intentionally;
this build-tooling pin does not alter application runtime roll-forward policy. Then run
the lock-file refresh commands above and commit the SDK input with the regenerated lock files.
Install servicing SDKs side-by-side: `bash setup-dev.sh --install-sdk` or
`.\setup-dev.ps1 -InstallSdk`. Do not remove the previous SDK until `main` and every active worktree
have stopped pinning it.

See `docs/CODE_QUALITY.md` for the full local tooling reference.
