# SharedKernel packaging

This page defines the local package shape for reusable `SharedKernel.*` projects.

## Package metadata

Shared package metadata defaults are centralized in the repository root `Directory.Build.props`:

- author and company values
- MIT license expression
- repository URL
- repository URL publishing
- symbol package generation with `.snupkg`

The `src/Directory.Build.props` file imports the root defaults for source projects. The
`src/Directory.Build.targets` file adds the source-wide AOT default after project target frameworks
are known. Source projects under `src/` default to AOT-compatible and must opt out explicitly when a
dependency, target framework, or project type cannot support that promise. Roslyn analyzer, code-fix,
and source-generator packages that target `netstandard2.0` opt out in their project files because the
.NET SDK does not support `IsAotCompatible` for that target framework.

Each packable project still owns its package-specific identity:

- `PackageId`
- `Description`
- `PackageTags`
- `PackageReadmeFile` when the package includes a README

Packable non-Roslyn `SharedKernel.*` source projects generate symbol packages with
`IncludeSymbols=true` and `SymbolPackageFormat=snupkg`. Roslyn analyzer, code-fix, and source-generator
packages keep their analyzer package layout separate. Project READMEs are included in packages with
`PackageReadmeFile` and a packed `README.md` item when the package has a README.

Reusable testing helper packages live under `src/SharedKernel` and are included in the same local
pack/restore validation as runtime packages. They opt out of source-wide AOT compatibility because
test frameworks, Roslyn test infrastructure, and Aspire test hosts are not production runtime
surfaces.

Package-worthy test helpers use `SharedKernel.Testing.<Capability>` names. Move a helper to
`src/SharedKernel` only when it exposes reusable behavior with a clear boundary outside this
repository's test taxonomy and has focused tests for its public behavior.

Current package-worthy testing helper packages:

| Package | Reusable boundary | Test coverage |
| --- | --- | --- |
| `SharedKernel.Testing.AspNetCore` | ASP.NET Core `WebApplicationFactory<TEntryPoint>` setup helpers. | Used by Catalog and Public web endpoint tests. |
| `SharedKernel.Testing.Mediator` | Reference mediator dispatcher used as a correctness oracle. | Covered by `tests/SharedKernel.Mediator.Tests`. |

Repository-only test helpers stay under `tests/` and are explicitly non-packable:

| Helper project | Placement decision |
| --- | --- |
| `SharedKernel.CodeFixes.Testing` | Repository-local Roslyn code-fix test support. |
| `SharedKernel.Testing.Contracts` | Repository-local contract test taxonomy. |
| `SharedKernel.Testing.Integration` | Repository-local integration test taxonomy. |
| `SharedKernel.Testing.Packaging` | Repository-local package-test builders. |
| `SharedKernel.Testing.Scenarios` | Repository-local scenario test taxonomy. |
| `SharedKernel.Testing.System` | Repository-local system test taxonomy. |

Samples and benchmarks are explicitly non-packable:

| Project | Reason |
| --- | --- |
| `samples/Mediator/Mediator.Sample` | Consumer-facing sample, not a reusable package. |
| `samples/Results/BasicResults.Sample` | Consumer-facing sample, not a reusable package. |
| `benchmarks/SharedKernel.Functional.Benchmarks` | Measurement harness, not a reusable package. |
| `benchmarks/SharedKernel.Mediator.Benchmarks` | Measurement harness, not a reusable package. |

## Naming

Use `SharedKernel.<Capability>` for provider-neutral packages.

Use `SharedKernel.<Capability>.<Provider>` for provider-specific adapters.

Use analyzer, code-fix, and source-generator suffixes only for Roslyn packages:

- `SharedKernel.<Capability>.Analyzers`
- `SharedKernel.<Capability>.CodeFixes`
- `SharedKernel.<Capability>.SourceGenerator`

Use `SharedKernel.Testing.<Capability>` for reusable test helpers. Keep repository-only test support
under `tests/` when it only encodes this repository's test taxonomy or fixtures.

Do not use `SharedKernel.<Capability>.Testing.*` for package-worthy helpers. That shape is reserved
for old repository-local helpers while they are being reviewed and should migrate to
`SharedKernel.Testing.<Capability>` before packaging.

Use `SharedKernel.<Capability>` for the primary package in a feature family. Use
`SharedKernel.<Capability>.<Submodule>` only for an optional surface with a real independent reason to
exist now, such as a provider adapter, Roslyn component, source generator, integration-specific
extension, or abstraction package that avoids a concrete implementation dependency.

Use `Abstractions` only when consumers need contracts without the implementation package and at least
two real consumers or implementations need that split now. Otherwise keep contracts in the primary
feature package. The primary feature package is the core surface; do not add `Core` packages unless a
documented migration or compatibility constraint prevents the root package from owning that surface.

Keep package IDs aligned with the root namespace unless a package has a documented compatibility reason
to differ.

Current package inventory follows these conventions:

| Package family | Location | Convention |
| --- | --- | --- |
| Runtime and provider packages | `src/SharedKernel/SharedKernel.*` | `PackageId` matches the project file name and folder name. |
| Roslyn analyzers, code fixes, and source generators | `src/SharedKernel/SharedKernel.*.{Analyzers,CodeFixes,SourceGenerator}` | `netstandard2.0`, Roslyn package suffix, and explicit `IsAotCompatible=false`. |
| Reusable testing helpers | `src/SharedKernel/SharedKernel.Testing*` | Packable only when intended for reuse outside this repository's test taxonomy. |
| Repo-owned command tools | `tools/SharedKernel.*` | Pack as .NET tools when useful outside one local `dotnet run --project` invocation. |

Intentional naming exceptions:

| Project | Exception | Reason |
| --- | --- | --- |
| `SharedKernel.Mediator.Abstractions` | Root namespace remains `SharedKernel.Mediator`. | The assembly contributes shared mediator contracts under the existing mediator namespace while keeping package identity precise. |
| `SharedKernel.EventSourcing.Npgsql` | Provider segment uses the provider name `Npgsql`. | Matches the .NET PostgreSQL provider package naming. |

Current deferred package candidates:

| Project | Decision |
| --- | --- |
| `tests/SharedKernel.CodeFixes.Testing` | Reusable Roslyn code-fix test workspace exists, but `SharedKernel.Testing.CodeFixes` is already a Roslyn code-fix package name. Extract to a collision-free package only after a dedicated naming/design decision. |

## Local package artifact layout

Local package builds write `.nupkg` files under an artifact directory outside source folders:

```text
artifacts/packages/local/<version>/
```

Use a unique prerelease version for local validation so NuGet cache reuse cannot hide package content
changes.

## Repo-owned tool package workflow

Repo-owned command-line automation is packaged as .NET tools when the executable is useful outside a
single `dotnet run --project` call. Current repo-owned tool package IDs and commands:

Use `tools/` for repository automation such as release, compatibility, code-fix, and packaging
commands. Use `src/SharedKernel/` only for reusable library APIs that consumers reference at runtime
or from tests. A command that orchestrates repository files, GitHub Actions artifacts, or `dotnet`
processes belongs under `tools/`, even when it primarily supports SharedKernel packages.

| Package ID | Command |
| --- | --- |
| `SharedKernel.Versioning.Tool` | `sharedkernel-version` |
| `SharedKernel.Testing.CodeFixRunner` | `sharedkernel-codefix` |

Pack tools into an ignored local feed with a unique prerelease version:

```bash
dotnet pack tools/SharedKernel.Versioning.Tool/SharedKernel.Versioning.Tool.csproj \
  --configuration Release \
  --output /tmp/opencode/sharedkernel-tools \
  -p:ComputedSemVer=0.1.0-alpha.local.20260705000000

dotnet pack tools/SharedKernel.Testing.CodeFixRunner/SharedKernel.Testing.CodeFixRunner.csproj \
  --configuration Release \
  --output /tmp/opencode/sharedkernel-tools \
  -p:ComputedSemVer=0.1.0-alpha.local.20260705000000
```

Install or update from that local feed only after trusting the package contents. Set `package_id` to
one of the repo-owned tool package IDs above:

```bash
package_id=SharedKernel.Versioning.Tool

dotnet tool install "${package_id}" \
  --tool-path /tmp/opencode/sharedkernel-tool-bin \
  --add-source /tmp/opencode/sharedkernel-tools \
  --version 0.1.0-alpha.local.20260705000000

dotnet tool update "${package_id}" \
  --tool-path /tmp/opencode/sharedkernel-tool-bin \
  --add-source /tmp/opencode/sharedkernel-tools \
  --version 0.1.0-alpha.local.20260705000000
```

When running from this repository, package source mapping in `NuGet.Config` intentionally blocks ad hoc
`--add-source` restores for unmapped package IDs. For install smoke checks, run from a scratch directory
with an explicit temporary NuGet config that maps the local feed to the repo-owned tool package IDs and
maps `nuget.org` to external dependencies.

Do not add repo-owned tools to `.config/dotnet-tools.json` until a stable trusted feed is available for
ordinary `dotnet tool restore` users.

## Local feed workflow

Pack all current source `SharedKernel.*` projects into a local artifact folder and verify that a
scratch project can restore them from that local feed:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- \
  pack-sharedkernel --version 0.1.0-alpha.local.20260704000000
```

If `--version` is omitted, the tool creates a timestamped local prerelease version. Reusing an
existing package version fails before packing when any `.nupkg`, `.snupkg`, or `.symbols.nupkg`
artifact already exists for that version. This keeps local and release dry runs from accidentally
hiding package-content changes behind NuGet cache reuse.

The restore check creates a scratch project under the artifact folder, references every generated
`SharedKernel.*` package, restores with the local feed plus `nuget.org`, and verifies each generated
package exists in the scratch package cache. The scratch restore uses a generated `NuGet.config` with
`<clear />`, exact package source mapping for the generated package IDs, a wildcard `nuget.org` mapping
for external dependencies, and a scratch package cache. This avoids inherited machine sources and keeps
local `SharedKernel.*` package resolution deterministic even if a colliding package ID exists on
`nuget.org`. Use `--skip-restore-check` only when diagnosing pack failures before restore validation is
relevant.

The restore verifier runs inside the .NET tool and invokes the local `dotnet restore` used for the
scratch project. This workflow intentionally validates the local .NET SDK/package environment that
produced the packages.

Use this workflow only for local validation and CI dry runs. Stable publishing needs release workflow
gates, release notes, provenance, and support-policy decisions.

## Release prep workflow

The `Release Prep` GitHub Actions workflow uses `SharedKernel.Versioning.Tool` to calculate the
release version, pack the current `SharedKernel.*` projects, validate package metadata, generate release
notes, write a changelog, and create provenance, attribution, and SBOM artifacts.

Stable release behavior is disabled by default. Creating a `vX.Y.Z` tag, creating a GitHub release,
or publishing to NuGet requires manual workflow dispatch and the `release` environment approval.
NuGet publishing also requires `NUGET_API_KEY` to be configured for that approved environment.

Release prep writes a minimal SPDX 2.3 SBOM from resolved `packages.lock.json` files to
`artifacts/release-prep/sbom.spdx.json`. It also writes `third-party-attributions.json` and
`third-party-notices.md` for release review. License fields that cannot be proven from lock files use
`NOASSERTION` and must be reviewed against NuGet and dependency-review metadata.

Validate package metadata before release work:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- validate-package-metadata
```

## Internal SharedKernel dependency versions

Packable `SharedKernel.*` projects should keep internal dependencies as project references in source.
During `dotnet pack`, those project references become package dependencies that use the same
`ComputedSemVer` as the package set being packed. The local feed verifier rejects generated packages
when an internal `SharedKernel.*` dependency does not match the pack version.

Do not widen internal dependency ranges in individual project files without a compatibility policy for
that package family. Broader ranges belong in a release-governed change because they let consumers mix
different SharedKernel package versions.

## Public API compatibility

Every `SharedKernel.*` source package has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` baseline
files. The repository enables `Microsoft.CodeAnalysis.PublicApiAnalyzers` centrally for SharedKernel
package projects, so new public APIs must be added to the baseline intentionally.

Run the baseline presence check before release work:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- check-public-api-baselines
```

Run the package compatibility report command before release work:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- api-compatibility
```

See [API compatibility gates](API_COMPATIBILITY.md) for alpha, beta, release-candidate, and stable
breaking-change expectations.
