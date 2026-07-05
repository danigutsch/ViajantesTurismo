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

## Naming

Use `SharedKernel.<Capability>` for provider-neutral packages.

Use `SharedKernel.<Capability>.<Provider>` for provider-specific adapters.

Use analyzer, code-fix, and source-generator suffixes only for Roslyn packages:

- `SharedKernel.<Capability>.Analyzers`
- `SharedKernel.<Capability>.CodeFixes`
- `SharedKernel.<Capability>.SourceGenerator`

Keep package IDs aligned with the root namespace unless a package has a documented compatibility reason
to differ.

## Local package artifact layout

Local package builds write `.nupkg` files under an artifact directory outside source folders:

```text
artifacts/packages/local/<version>/
```

Use a unique prerelease version for local validation so NuGet cache reuse cannot hide package content
changes.

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
