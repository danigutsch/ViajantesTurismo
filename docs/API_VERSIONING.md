# API versioning

API versions are public contract versions. They do not follow application, assembly, or NuGet package versions.

## Version independence

- Increment an API version only when the HTTP contract needs a compatibility boundary.
- Do not sync API versions with package releases such as `SharedKernel.ApiVersioning` or application SemVer.
- Package versions describe library delivery. API versions describe consumer-facing request and response contracts.

## SharedKernel packages

- `SharedKernel.ApiVersioning` owns host-agnostic API version, lifecycle, deprecation, and selection primitives.
- `SharedKernel.ApiVersioning.AspNetCore` owns ASP.NET Core route metadata and `/api/v1` style route helpers.
- `SharedKernel.OpenApi` owns OpenAPI document registration for versioned API contracts.

Keep app-specific boundary names, domain policy, and business compatibility rules in the consuming application.

## Deprecation

Use `ApiVersionStatus.Deprecated` for versions that remain selectable but should move consumers forward. Use
`ApiDeprecationPolicy` for sunset dates or migration information. Use `ApiVersionStatus.Retired` only when the version
must no longer be selected for requests.

## Value-object generation note

`ApiVersion` is value-object-shaped. It is implemented explicitly today so API versioning packages can ship without
waiting for the source-generation epic. Future work is tracked in #460 and #750 for Vogen-like value-object generation
and specialized API version generation.
