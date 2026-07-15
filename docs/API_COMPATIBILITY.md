# API compatibility gates

This repository protects two public contract surfaces:

- `SharedKernel.*` package APIs through `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`,
  and .NET package validation.
- Admin, Catalog, and Branding HTTP contracts through canonical OpenAPI artifacts and contract tests.

## Release phase policy

| Phase | Gate behavior | Breaking-change expectation |
| --- | --- | --- |
| Alpha / `0.y.z` / `-alpha` | Report-only compatibility artifacts | Breaking changes are allowed. Keep the package/API visibly prerelease and capture important design churn in release notes or issues. |
| Beta / `-beta` | Report-only only when a breaking-change marker is present | Breaking changes are allowed, but should be intentional and documented. |
| Release candidate / `-rc` | Block breaking diffs | Treat the API as final except for critical fixes. |
| Stable / `>=1.0.0` | Block breaking diffs, even when a marker is present | Use a SemVer major version or a new HTTP API version for incompatible changes. Deprecate first when practical. |

This follows SemVer's rule that `0.y.z` is initial development and anything may
change, NuGet's prerelease opt-in model, and Microsoft's guidance to minimize stable
library breaking changes.

References:

- [Semantic Versioning](https://semver.org/)
- [NuGet package versioning](https://learn.microsoft.com/nuget/concepts/package-versioning)
- [.NET library guidance for breaking changes](https://learn.microsoft.com/dotnet/standard/library-guidance/breaking-changes)
- [.NET library change rules](https://learn.microsoft.com/dotnet/core/compatibility/library-change-rules)
- [.NET package validation](https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview)

## Local checks

Run the public API baseline presence check:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- check-public-api-baselines
```

Run the package compatibility/report command:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- api-compatibility
```

To compare against a previous package version when it is available in the NuGet cache or configured
sources, set:

```bash
dotnet run --project tools/SharedKernel.Versioning.Tool -- \
  api-compatibility --baseline-version 0.1.0-alpha.0
```

Compatibility output is written under `artifacts/api-compat/` for CI artifact upload.

When `--baseline-version` is supplied, the command maps its internal settings to the official .NET SDK
package validation properties used by `dotnet pack`:

| Internal setting | Environment variable | MSBuild property |
| --- | --- | --- |
| Enable package validation | `API_COMPAT_ENABLE_PACKAGE_VALIDATION` | `EnablePackageValidation` |
| Baseline package version | `API_COMPAT_BASELINE_VERSION` | `PackageValidationBaselineVersion` |

These environment variables are scoped inside `SharedKernel.Versioning.Tool`; the tool restores any
previous values after the report command finishes. Contributors should prefer the command-line options
above instead of setting these variables directly.

## Breaking-change marker

When a beta compatibility check finds a breaking API diff, the PR must include a Conventional Commit
breaking marker in at least one commit:

```text
feat(api)!: remove legacy contract
```

or a footer:

```text
BREAKING CHANGE: describe the migration impact
```

Alpha changes do not need this marker unless the author wants the release notes to call out an
important consumer-visible break.

Release-candidate and stable compatibility failures still block even when a marker is present.

## OpenAPI snapshots

The Admin, Catalog, and Branding contract tests read the committed OpenAPI artifacts and compare them
with generated documents. Regenerate the committed artifacts intentionally when HTTP contracts change:

After a locked restore, the .NET tool runs the supported build generator with a minimal child environment.
Trusted generation omits JWT/OIDC registration while retaining authorization metadata for the generated
Bearer security documents. Normal application runs remain fail-closed when authentication configuration
is missing.

```bash
dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate admin --refresh
dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate catalog --refresh
dotnet run --project tools/ViajantesTurismo.OpenApi.Tool --no-restore -- generate branding --refresh
```

Then run:

```bash
dotnet test --project tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj --filter-class "*OpenApi*"
dotnet test --project tests/ViajantesTurismo.Catalog.ContractTests/ViajantesTurismo.Catalog.ContractTests.csproj --filter-class "*OpenApi*"
dotnet test --project tests/ViajantesTurismo.Branding.ContractTests/ViajantesTurismo.Branding.ContractTests.csproj --filter-class "*OpenApi*"
```
