# ViajantesTurismo.Architecture.Analyzers

App-owned Roslyn analyzers for ViajantesTurismo project and resource boundaries. These rules are
application architecture policy and do not belong in reusable SharedKernel packages or global style
analyzers.

## `VTARCH001`: production API-to-API HTTP

`VTARCH001` reports ordinary HTTP communication from one repository-owned production `Api`
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

The analyzer is referenced explicitly by each production `Api` project (Admin and Catalog), which
build with warnings as errors:

```xml
<ProjectReference Include="..\ViajantesTurismo.Architecture.Analyzers\ViajantesTurismo.Architecture.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  PrivateAssets="all"
                  IsAspireProjectResource="false" />
```

Catalog.Api hosts the Branding routes, so Branding HTTP clients and contracts resolve to the Catalog
API destination. Keep production API references project-local.

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

## `VTARCH004`: HTTP transport in tool wrappers

Use conventional `.csproj` tool projects under `scripts/` so solution builds, locked restores,
and command contract tests use the same entry points. Enable `VTARCH004` in each tool project
by referencing the architecture analyzer and exposing the tool property to the compiler.

```xml
<PropertyGroup>
    <ViajantesTurismoToolWrapper>true</ViajantesTurismoToolWrapper>
</PropertyGroup>
<ItemGroup>
    <CompilerVisibleProperty Include="ViajantesTurismoToolWrapper" />
    <ProjectReference Include="../../src/ViajantesTurismo.Architecture.Analyzers/ViajantesTurismo.Architecture.Analyzers.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false"
                      PrivateAssets="all"
                      IsAspireProjectResource="false" />
</ItemGroup>
```

The rule reports framework HTTP calls through `HttpClient`, `HttpMessageInvoker`, and the
HTTP JSON extensions. Move request execution into a client under `src/` and invoke that client
from the wrapper. Forge commands can compose the shared GitHub client. The wrapper owns
arguments, configuration, dependency composition, and presentation.
Creating or configuring a client for composition is permitted.

## `VTARCH005` helper placement

Use the advisory analyzer when architecture maintainers have identified a source or test helper for
review. Opt in on the helper's project and identify the top-level type and reusable capability.
Keep the capability inventory with the application decision.

```xml
<PropertyGroup>
    <ViajantesTurismoSharedKernelHelperCandidates>Example.ElapsedHelper=clock</ViajantesTurismoSharedKernelHelperCandidates>
    <ViajantesTurismoSharedKernelCapabilityOwners>clock=PandaBear.SharedKernel.Scheduling</ViajantesTurismoSharedKernelCapabilityOwners>
</PropertyGroup>
<ItemGroup>
    <CompilerVisibleProperty Include="ViajantesTurismoSharedKernelHelperCandidates" />
    <CompilerVisibleProperty Include="ViajantesTurismoSharedKernelCapabilityOwners" />
</ItemGroup>
```

Use `|` between multiple entries and include the project-local analyzer reference shown above.
The analyzer resolves aliases, generic arguments and every partial declaration. It reports the
referenced types when they come from .NET base libraries or SharedKernel, with xUnit allowed for a
test project. References to application or other provider types keep the helper in its owning project.

Use a registered package for an exact capability owner. An unregistered capability prompts a
review for a focused library. Verify reuse and destination compatibility before moving code.
See the [coupling and cohesion proposal](../../docs/architecture/coupling-and-cohesion-metrics.md).
