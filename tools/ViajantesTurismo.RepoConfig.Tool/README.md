# ViajantesTurismo repository automation

This dependency-independent application tool owns `bootstrap-sharedkernel`,
`verify-sharedkernel-candidate`, `select-ci-test-projects`, `verify-coverage`, and `lint-json`.
Run it with `dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -- <command>`.

`lint-json` validates the working-tree contents of Git-indexed `*.json` files with comments and
trailing commas allowed. It requires UTF-8, accepts a UTF-8 BOM, and reports every invalid file.
Untracked caches, downloaded packages, and nested library checkouts are outside this policy.

Generic configuration, roadmap, GitHub projection, encoding, and Podman diagnostics live in
`PandaBear.SharedKernel.RepoConfig.Tool`, restored through the local tool manifest and invoked
with `dotnet tool run sharedkernel-repo -- <command>`.
Documentation automation similarly uses `dotnet tool run sharedkernel-docs -- <command>`.

See [SharedKernel consumption](../../docs/sharedkernel/development.md) for bootstrap and validation.
