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

## Application route strategy

ViajantesTurismo app APIs use URL path versioning. The current public contract version is `v1`, exposed under
`/api/v1/...`.

Current route prefixes:

- Admin: `/api/v1/tours`, `/api/v1/customers`, `/api/v1/customers/import`, `/api/v1/bookings`, and
  `/api/v1/docs/errors`
- Catalog management: `/api/v1/catalog/...`
- Public catalog: `/api/v1/public/catalog/...`

The unversioned API routes are not part of the supported HTTP contract. Browser-facing Management and Public Web routes
remain unversioned UI routes; typed API clients translate API calls to the versioned service endpoints.

OpenAPI artifacts keep the existing boundary files (`tours`, `customers`, `bookings`, `catalog`, and `public-catalog`)
and add a service-wide `v1` document for version-level review. Refresh committed artifacts only when the HTTP contract
changes intentionally.

## Deprecation

Use `ApiVersionStatus.Deprecated` for versions that remain selectable but should move consumers forward. Use
`ApiDeprecationPolicy` for sunset dates or migration information. Use `ApiVersionStatus.Retired` only when the version
must no longer be selected for requests.

Additive changes stay on the current version when they do not break existing clients. Breaking changes require a new
route segment such as `/api/v2`, with the older version marked deprecated before retirement whenever practical.

## Value-object generation note

`ApiVersion` is value-object-shaped. It is implemented explicitly today so API versioning packages can ship without
waiting for the source-generation epic. Future work is tracked in #460 and #750 for Vogen-like value-object generation
and specialized API version generation.
