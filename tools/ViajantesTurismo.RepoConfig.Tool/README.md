# ViajantesTurismo repository automation

This dependency-independent application tool owns
`verify-sharedkernel-candidate`, `select-ci-test-projects`, `verify-coverage`, and `lint-json`.
Run it with `dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -- <command>`.

`lint-json` validates the working-tree contents of Git-indexed `*.json` files with comments and
trailing commas allowed. It requires UTF-8, accepts a UTF-8 BOM, and reports every invalid file.
Untracked caches, downloaded packages, and nested library checkouts are outside this policy.

Generic configuration, roadmap, GitHub projection, encoding, and Podman diagnostics live in
`PandaBear.SharedKernel.RepoConfig.Tool`, restored through the local tool manifest and invoked
with `dotnet tool run sharedkernel-repo -- <command>`.
Documentation automation similarly uses `dotnet tool run sharedkernel-docs -- <command>`.

See [SharedKernel consumption](../../docs/sharedkernel/development.md) for candidate staging and validation.

## CI commands

`run-ci-test-slice --slice-name <name> --projects-file <manifest> [--install-playwright]` builds
complete solution-owned test projects, prepares their runtime prerequisites, and invokes the
existing coverage collector. It retains per-phase logs, TSV timings, and a JSON failure/success
manifest under `TestResults`, including the tested revision. Restore the selected projects first.

`write-build-evidence` records the checkout revision and SHA-256 hashes of Release PDBs under
`src` and `tools`. `verify-build-evidence` rejects stale revisions, changed or undeclared symbols,
and paths outside the checkout. Download the artifact to the checkout root before verification.

`plan-dependency-review --base <sha> --head <sha> [--github-output <path>]` compares exact Git
revisions and records its decision in `TestResults/dependency-review-plan.json`. Only standalone
Markdown/images under `docs`, root `README.md`, and root `AGENTS.md` can skip dependency review.
Source changes, package READMEs, pins, unknown paths, and unavailable comparisons retain full review.

`restore-ci-projects --projects-file <manifest>` or `--targets <newline-separated paths>` restores
project roots as one temporary MSBuild solution graph. Solution targets retain their normal restore
behavior. Paths and symbolic links cannot escape the checkout; an empty target list fails. The
setup action runs this command after staging the candidate, which builds the dependency-independent
repository tool for later `--no-build` steps.
