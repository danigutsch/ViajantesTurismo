# ViajantesTurismo.Architecture.Analyzers

App-owned Roslyn analyzers for ViajantesTurismo project and resource boundaries. These rules are
application architecture policy and do not belong in reusable SharedKernel packages or global style
analyzers.

## `VTARCH001`: production API-to-API HTTP

`VTARCH001` reports ordinary HTTP communication from one repository-owned production `ApiService`
to another. Approved communication uses a versioned integration event, explicitly approved gRPC, or
an in-process module call.

The analyzer resolves exact repository symbols, unmodified local aliases, and absolute URI authorities
for known static destinations. It does not guess values loaded from runtime configuration, infer a
destination from a relative path or similar type name, ban `HttpClient` globally, make runtime protocol
claims, or ship a migration code fix.

### Exact-site exception

An approved exception must suppress one allowlisted statement, name `VTARCH001`, and include a
code-owner-reviewed justification:

```csharp
#pragma warning disable VTARCH001 // Justification: approved synchronous exception reviewed by architecture owners.
services.AddHttpClient(ResourceNames.BrandingApi);
#pragma warning restore VTARCH001
```

Project-wide `NoWarn`, `WarningsNotAsErrors`, and `.editorconfig` severity suppression are prohibited.
`SuppressMessage` and `UnconditionalSuppressMessage` attributes are also prohibited for this rule.

## Staged rollout: not enabled

This project and its tests land before enforcement. Do not reference the analyzer from production
projects while the Admin-to-Branding HTTP edge remains. Activate only after the asynchronous Admin
Branding projection removes that dependency and Catalog co-hosting retires the standalone Branding
API. Then add the analyzer explicitly to each remaining production `ApiService` project:

```xml
<ProjectReference Include="..\ViajantesTurismo.Architecture.Analyzers\ViajantesTurismo.Architecture.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  PrivateAssets="all"
                  IsAspireProjectResource="false" />
```

Do not add this reference to `Directory.Build.props`, tests, AppHost, tools, frontends, or external-
provider projects. The later activation commit must update the architecture rollout test after all
production `ApiService` projects build cleanly with warnings as errors.
