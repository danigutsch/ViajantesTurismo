# ViajantesTurismo.Architecture.Analyzers

App-owned Roslyn analyzers for ViajantesTurismo project and resource boundaries. These rules are
application architecture policy and do not belong in reusable SharedKernel packages or global style
analyzers.

## `VTARCH001`: production API-to-API HTTP

`VTARCH001` reports ordinary HTTP communication from one repository-owned production `ApiService`
to another. Approved communication uses a versioned integration event, explicitly approved gRPC, or
an in-process module call.

The analyzer resolves exact repository symbols, definite local reaching definitions, recursively
composed static URI components, and resource tokens in absolute URI authorities for known static
destinations. Resource-like path, query, and fragment values are not destinations. The analyzer does
not guess values loaded from runtime configuration, infer a destination from a relative request URI or
similar type name, ban `HttpClient` globally, make runtime protocol claims, or ship a migration code fix.
Bare repository resource symbols count only in named-client and `BaseAddress` resolution; request URI
positions require an absolute authority.

### Exact-site exception

An approved exception must suppress one allowlisted statement with exactly one eligible diagnostic
site, name `VTARCH001`, contain no nested expression control flow or nested destination assignment, and
include a code-owner-reviewed justification:

```csharp
#pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
services.AddHttpClient(ResourceNames.CatalogApi);
#pragma warning restore VTARCH001
```

Project-wide `NoWarn` and `.editorconfig` severity suppression, any `WarningsNotAsErrors` declaration,
and disabling `RunAnalyzers` or `RunAnalyzersDuringBuild` are prohibited. `SuppressMessage` and
`UnconditionalSuppressMessage` attributes are also prohibited for this rule. Global aliases to those
attributes, unresolved suppression check IDs, `GlobalAnalyzerConfigFiles` and `EditorConfigFiles`
inputs, and `CodeAnalysisRuleSet` inputs are prohibited as broad suppression paths.

The centrally pinned `Microsoft.CodeAnalysis.PublicApiAnalyzers` package enforces the committed
`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` baselines for this project's public analyzer APIs.

## Enforcement

The analyzer is referenced explicitly by each production `ApiService` project (Admin and Catalog), which
build with warnings as errors:

```xml
<ProjectReference Include="..\ViajantesTurismo.Architecture.Analyzers\ViajantesTurismo.Architecture.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  PrivateAssets="all"
                  IsAspireProjectResource="false" />
```

Catalog.ApiService hosts the Branding routes, so Branding HTTP clients and contracts resolve to the Catalog
API destination. Do not add this reference to `Directory.Build.props`, tests, AppHost, tools, frontends, or
external-provider projects.

## `VTARCH002`: integration event names

`VTARCH002` reports a concrete class, record, or struct that implements
`PandaBear.Messaging.IntegrationEvents.IIntegrationEvent` and whose name does not end with
`IntegrationEvent`. Abstract types and interfaces are not reported. The analyzer is referenced by the
`*.Contracts.IntegrationEvents` projects, where the events are declared.

## `VTARCH003`: domain error class names

`VTARCH003` reports a top-level static class in a `*.Domain` assembly whose public methods all return
`PandaBear.Results.Result` or `Result<T>` and whose name does not end with `Errors`. A static class that
mixes result-returning methods with other return types, or has no public methods, is not an error class
and is not reported. The analyzer is referenced by the `*.Domain` projects.

Both rules are enabled project by project with the same `OutputItemType="Analyzer"` project reference as
`VTARCH001`, never centrally.
