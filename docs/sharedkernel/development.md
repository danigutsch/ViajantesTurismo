# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. C# namespaces use `PandaBear.*`; assembly and project names remain `SharedKernel.*`.
Generic documentation and repository-configuration tooling belongs to SharedKernel. Application
documentation configuration, CI and coverage policy, bootstrap, and compatibility orchestration stay here.

The pinned candidate is `1.0.0-alpha.20261021.2` at library revision
`f5975b60601e245bda6bb8e45559d8ce3a24a8e0`. Its manifest covers 73 immutable packages; the application
references only the modules it uses. The Results runtime includes its generator, so do not add a
separate Results generator package.

CSV structure parsing and advisory-lock hashing use the Tabular and Npgsql packages. Application
import interfaces, accepted headers, normalization, business errors, and lock identities remain local.
The CSV parser preserves the existing simple comma-splitting behavior; quoted commas, escaping, and
multiline fields remain unsupported.

## Bootstrap a checkout

Install the SDK pinned by `global.json`. Clone the public
[PandaBear.SharedKernel source](https://github.com/danigutsch/PandaBear.SharedKernel), check out the
exact `libraryRevision` in `.config/sharedkernel-candidate.json`, and keep that checkout clean.
The committed candidate manifest pins the library revision, package version, and SHA-256 hashes.

Download the unchanged 73 `.nupkg` files and `candidate.json` from
[GitHub preview `v1.0.0-alpha.20261021.2`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v1.0.0-alpha.20261021.2)
into an explicit candidate feed directory. For example, with GitHub CLI:

```sh
gh release download v1.0.0-alpha.20261021.2 --repo danigutsch/PandaBear.SharedKernel \
  --pattern '*.nupkg' --pattern candidate.json --dir /path/to/candidate-packages
```

These GitHub prerelease assets are available for consumption; NuGet registry publication remains
deferred. Bootstrap validates the downloaded packages against the committed manifest before staging.

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

Generic outbox and idempotency implementation tests live in the library persistence test project.
The transfer preserves 37 guards in library test commit `3752abb54681e28f4d16abd29966bab3819dd0ea`;
application tests continue to verify domain dispatch, saved envelopes, registration, and schema policy
through public package interfaces. This test-only commit does not change the pinned runtime artifacts.

## CI and platform evidence

Hosted Linux and Windows jobs read the committed candidate manifest, check out the public library at
its exact `libraryRevision`, and download the unchanged packages and `candidate.json` from GitHub
prerelease `v<packageVersion>`. The approved candidate is distributed as release assets at
[`v1.0.0-alpha.20261021.2`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v1.0.0-alpha.20261021.2).
Checkout credentials are not persisted. CI downloads the manifest and exact pinned package filenames
through public release URLs without a token, avoiding per-asset installation API rate limits. Download
failures are retried three times; bootstrap still verifies the revision and every package hash before
restore. Source and downloads stay in ignored `.local/sharedkernel-library`
and `.local/sharedkernel-download` directories.
The shared prerequisite action invokes the same C# bootstrap before restoring application dependencies;
the committed revision and package hashes remain authoritative. Missing assets, revision mismatches,
and hash mismatches fail visibly.

Optional repository variables `SHAREDKERNEL_LIBRARY_ROOT` and `SHAREDKERNEL_CANDIDATE_FEED` retain the
explicit local/private flow. A library-root override skips hosted checkout and downloads, using that
checkout's standard candidate output directory unless a feed override is also provided. A feed-only
override uses the automatically checked-out pinned library with the specified artifacts.

Linux and Windows must consume the same hashed packages. Record platform tests and NativeAOT results
separately; an unavailable platform remains pending. Registry publication remains deferred: this
workflow consumes GitHub release assets and publishes nothing to nuget.org.

Generic commands use `dotnet tool run sharedkernel-repo -- <command>` and documentation commands use
`dotnet tool run sharedkernel-docs -- <command>`. Endpoint metadata methods and audience classification
are consumer settings in `docs/architecture/generated-diagrams.json`. Shared telemetry privacy processors
always enforce the baseline, with additional business-sensitive attribute policy supplied by ServiceDefaults.
