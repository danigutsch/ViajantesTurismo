# SharedKernel consumption

SharedKernel implementations, reusable test helpers, analyzers, generators, samples, benchmarks,
and library tooling belong to the separate SharedKernel repository. ViajantesTurismo consumes
`PandaBear.SharedKernel.*` packages. C# namespaces use `PandaBear.*`; assembly and project names remain `SharedKernel.*`.
Generic documentation and repository-configuration tooling belongs to SharedKernel. Application
documentation configuration, CI and coverage policy, bootstrap, and compatibility orchestration stay here.

The published DDD standards candidate is `0.1.0-alpha.133.1` at clean library revision
[`078adf2b2b158f38a211f48274af548d12a85752`](https://github.com/danigutsch/PandaBear.SharedKernel/commit/078adf2b2b158f38a211f48274af548d12a85752).
It adds the domain encapsulation analyzers and setter code fix that every `src/*.Domain` project
references, plus the library changes merged since `0.1.0-alpha.112.1`. All 77 package hashes match the
unchanged artifacts from
[CI run 40070217254](https://github.com/danigutsch/PandaBear.SharedKernel/actions/runs/40070217254);
Linux/Windows package consumption and NativeAOT passed. Isolated application compatibility at
consumer revision `b04552cd4c408d8c3c2c9258e3b4f14460e1c394` passed locked restore, Release build
with zero warnings/errors, OpenAPI generation and 4,055 tests with five optional fastText skips. The
unchanged 77 packages, 54 symbol archives and manifest are published in GitHub prerelease
[`v0.1.0-alpha.133.1`](https://github.com/danigutsch/PandaBear.SharedKernel/releases/tag/v0.1.0-alpha.133.1).

The previous previews `0.1.0-alpha.112.1` at revision `e4d13f63ff872a86478a16e5a3c3ce1ecba0fcb1` and
`0.1.0-alpha.94.1` at revision `d08008cd3e8901c304377edcc4683de7c3a85ecb` remain published. Their
application validation is historical evidence; neither contains the domain analyzers.

The former candidate `1.2.0-alpha.20261023.1` at library revision
`cc826be664bfc2dc087fe5a140f7232b4f65967d` is retired. Its release was deliberately deleted;
do not recreate, download, restore, or execute its artifacts. Earlier compatibility and package
validation results are historical evidence, not approval to consume a retired candidate.

HTTP test factories apply `AddTestRequestBindingDefaults` from `PandaBear.Testing.AspNetCore`.
Direct TestServer and Kestrel test hosts register the same extension. Invalid bodies return HTTP 400
in Development and Production test environments; ordinary application startup keeps framework defaults.

Dependency review retains main's separately reviewed immutable generator source revision. Its shared
`dependency snapshot` command compares committed lockfiles without restoring historical packages.
The application-owned duplicate generator has been removed. Review generator source independently
of the runtime package pin.
The Results runtime includes its generator, so do not add a separate Results generator package.

CSV structure parsing and advisory-lock hashing use the Tabular and Npgsql packages. Application
import interfaces, accepted headers, normalization, business errors, and lock identities remain local.
The CSV parser preserves the existing simple comma-splitting behavior; quoted commas, escaping, and
multiline fields remain unsupported.

## Bootstrap a checkout

Install the SDK pinned by `global.json`. Clone the public
[PandaBear.SharedKernel source](https://github.com/danigutsch/PandaBear.SharedKernel), check out the
exact `libraryRevision` in `.config/sharedkernel-candidate.json`, and keep that checkout clean.
The committed candidate manifest pins the library revision, package version, and SHA-256 hashes.

Use only the committed manifest's exact `.nupkg` files and `candidate.json` from the approved
`v<packageVersion>` release. The published files are unchanged from the validated fresh CI candidate. Supply a clean checkout of the exact library
revision and unchanged artifacts through explicit library/feed paths. Do not substitute another
preview's package files or use cached retired artifacts to bypass missing releases.

From the application root, stage the exact candidate with the dependency-independent C# tool:

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

Compatibility records both committed repository revisions and package hashes. Validation runs in a
temporary local clone of the recorded application commit with an isolated package cache, then removes
both. The working checkout's restore assets and binaries remain unchanged. A renamed package
identity does not establish compatibility with previously published `SharedKernel.*` packages.
Rollback may use only an approved, non-retired compatible candidate after validation. Update the
application dependency configuration, candidate manifest, and lock files together, then bootstrap
that candidate's exact artifacts. Retired releases must remain deleted and must not be consumed again.

Generic outbox and idempotency implementation tests live in the library persistence test project.
The transfer preserves 37 guards in library test commit `3752abb54681e28f4d16abd29966bab3819dd0ea`;
application tests continue to verify domain dispatch, saved envelopes, registration, and schema policy
through public package interfaces. This test-only commit does not change the pinned runtime artifacts.

## CI and platform evidence

Hosted Linux and Windows jobs read the committed candidate manifest, check out the public library at
its exact `libraryRevision`, and download unchanged packages and `candidate.json` from GitHub
prerelease `v<packageVersion>`. The published `0.1.0-alpha.133.1` assets match the committed candidate
hashes. Missing retired releases must not be recreated to unblock checks. The secret-scan
workflow also pins the shared scanner action to an immutable library revision. Hosted checks verify
the same immutable package bytes as local validation.

Checkout credentials are not persisted. CI downloads the manifest and exact pinned filenames through
public release URLs without a token. Download failures are retried three times; bootstrap still verifies
the revision and every package hash before restore. Source and downloads stay in ignored
`.local/sharedkernel-library` and `.local/sharedkernel-download` directories. Missing assets, revision
mismatches, and hash mismatches fail visibly.

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

## Shared security tooling

Security baseline checking and deterministic scanner orchestration are provided by the pinned
`sharedkernel-repo security` commands. Application document contracts, endpoint policies, CI
permissions, and runtime security tests remain here. SharedKernel also uses the same implementation
for its own baseline. The Linux scanner installer pins and verifies Gitleaks separately from NuGet
packages; see [the application baseline](../security/security-baseline.md).
