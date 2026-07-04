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

## Pack command

Pack all current source `SharedKernel.*` projects into a local artifact folder:

```bash
for project in src/SharedKernel/*/*.csproj; do
    dotnet pack "$project" \
        -c Release \
        -p:ComputedSemVer=0.1.0-alpha.local \
        -o artifacts/packages/local/0.1.0-alpha.local
done
```

Use this only for local validation and CI dry runs. Stable publishing needs release workflow gates,
release notes, provenance, and support-policy decisions.
