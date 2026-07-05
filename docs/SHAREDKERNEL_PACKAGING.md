# SharedKernel packaging

This page defines the local package shape for reusable `SharedKernel.*` projects.

## Package metadata

Shared package metadata defaults are centralized in the repository root `Directory.Build.props`:

- author and company values
- MIT license expression
- repository URL
- repository URL publishing

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

Reusable testing helper packages live under `src/SharedKernel` and are included in the same local
pack/restore validation as runtime packages. They opt out of source-wide AOT compatibility because
test frameworks, Roslyn test infrastructure, and Aspire test hosts are not production runtime
surfaces.

## Naming

Use `SharedKernel.<Capability>` for provider-neutral packages.

Use `SharedKernel.<Capability>.<Provider>` for provider-specific adapters.

Use analyzer, code-fix, and source-generator suffixes only for Roslyn packages:

- `SharedKernel.<Capability>.Analyzers`
- `SharedKernel.<Capability>.CodeFixes`
- `SharedKernel.<Capability>.SourceGenerator`

Use `SharedKernel.Testing.<Capability>` for reusable test helpers. Keep repository-only test support
under `tests/` when it only encodes this repository's test taxonomy or fixtures.

Keep package IDs aligned with the root namespace unless a package has a documented compatibility reason
to differ.

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
release version, pack the current `SharedKernel.*` projects, generate release notes, write a
changelog, and create a minimal provenance manifest with package SHA-256 hashes.

Stable release behavior is disabled by default. Creating a `vX.Y.Z` tag, creating a GitHub release,
or publishing to NuGet requires manual workflow dispatch and the `release` environment approval.
NuGet publishing also requires `NUGET_API_KEY` to be configured for that approved environment.

Formal SBOM output remains a follow-up because no repository-approved SBOM generator is currently
wired into local tooling or CI.

## Internal SharedKernel dependency versions

Packable `SharedKernel.*` projects should keep internal dependencies as project references in source.
During `dotnet pack`, those project references become package dependencies that use the same
`ComputedSemVer` as the package set being packed. The local feed verifier rejects generated packages
when an internal `SharedKernel.*` dependency does not match the pack version.

Do not widen internal dependency ranges in individual project files without a compatibility policy for
that package family. Broader ranges belong in a release-governed change because they let consumers mix
different SharedKernel package versions.
