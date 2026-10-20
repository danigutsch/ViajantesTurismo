# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. Public namespaces and assembly names remain `SharedKernel.*`.
Application documentation and repository tooling belong to `ViajantesTurismo.*` projects here.

## Bootstrap a checkout

Install the SDK pinned by `global.json`. Obtain a clean SharedKernel checkout at the revision in
`.config/sharedkernel-candidate.json`, together with the unchanged candidate package artifacts.
The candidate manifest pins the library revision, package version, and SHA-256 hashes. The library
repository has no configured remote yet; no command creates one or publishes packages.

From the application root, stage the expected candidate with the dependency-independent C# tool:

```sh
dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -c Release -- \
  bootstrap-sharedkernel --library-root /path/to/SharedKernel --feed /path/to/candidate-packages
dotnet tool restore
dotnet restore ViajantesTurismo.slnx --locked-mode
dotnet build ViajantesTurismo.slnx -c Release --no-restore
dotnet run --project tools/ViajantesTurismo.OpenApi.Tool -c Release --no-build --no-restore -- generate all
dotnet test --solution ViajantesTurismo.slnx -c Release --no-build --no-restore
```

Omit `--feed` when the artifacts are in the library checkout's standard candidate output directory.
Both paths are explicit runtime inputs; no committed project reference crosses repository boundaries.
Bootstrap checks the clean library revision and every package hash, then stages into the ignored
`.local/sharedkernel-feed`. It refuses different existing artifacts and removes incomplete temporary
copies on cancellation. Repeating it with identical packages is safe. Package source mapping sends
`PandaBear.SharedKernel.*` exclusively to that local feed, preventing public-feed fallback. Third-party
packages and vulnerability auditing continue to use nuget.org.

## Adopt a library change

Commit and validate library changes first. Use its C# `validate-candidate` command to create a new,
unique prerelease candidate. Never overwrite an existing candidate version. Run the library's full
suite, API checks, package-consumption checks, and promised NativeAOT checks before adoption.

Update the committed candidate manifest, central package versions, local tool versions, and lock files
in an application worktree. Bootstrap the exact artifacts, run the application checks above, commit
those changes, and record compatibility using:

```sh
dotnet run --project tools/ViajantesTurismo.RepoConfig.Tool -c Release --no-build --no-restore -- \
  verify-sharedkernel-candidate --candidate .local/sharedkernel-feed/candidate.json
```

Compatibility records both committed repository revisions and package hashes. A renamed package
identity does not establish compatibility with previously published `SharedKernel.*` packages.
Rollback restores the previous application dependency configuration, candidate manifest, and lock files,
then bootstraps its retained artifacts. Keep the prior artifacts available until adoption is accepted.

## CI and platform evidence

Runners must have a clean checkout of the pinned library revision. Configure repository variables
`SHAREDKERNEL_LIBRARY_ROOT` and, when artifacts are elsewhere, `SHAREDKERNEL_CANDIDATE_FEED`.
The shared prerequisite action invokes the same C# bootstrap before restoring application dependencies.
Missing source, missing artifacts, revision mismatches, and hash mismatches fail visibly.

Linux and Windows must consume the same hashed packages. Record platform tests and NativeAOT results
separately; an unavailable platform remains pending. Hosted distribution and public publishing require
a later explicit decision. This workflow uses no package server and publishes nothing to nuget.org.
