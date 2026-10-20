# ViajantesTurismo repository automation

This dependency-independent application tool owns `bootstrap-sharedkernel`,
`verify-sharedkernel-candidate`, `select-ci-test-projects`, and `verify-coverage`.
Run it with `dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -- <command>`.

Generic configuration, roadmap, GitHub projection, encoding, and Podman diagnostics live in
`PandaBear.SharedKernel.RepoConfig.Tool`, restored through the local tool manifest and invoked
with `dotnet tool run sharedkernel-repo -- <command>`.
Documentation automation similarly uses `dotnet tool run sharedkernel-docs -- <command>`.

See [SharedKernel consumption](../../docs/sharedkernel/development.md) for bootstrap and validation.
