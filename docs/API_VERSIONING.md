# API versioning

API versions are public contract versions. They do not follow application, assembly, or NuGet package versions.

## Version independence

- Increment an API version only when the HTTP contract needs a compatibility boundary.
- Do not sync API versions with package releases such as `SharedKernel.ApiVersioning` or application SemVer.
- Package versions describe library delivery. API versions describe consumer-facing request and response contracts.

## SharedKernel packages

- `SharedKernel.ApiVersioning` owns host-agnostic API version, lifecycle, deprecation, and selection primitives.
- `SharedKernel.ApiVersioning.AspNetCore` owns generic ASP.NET Core route metadata and version-group
  composition helpers. It does not own application API prefixes or module route segments.
- `SharedKernel.OpenApi` owns OpenAPI document registration for versioned API contracts.

Keep app-specific boundary names, domain policy, and business compatibility rules in the consuming application.

## Application route strategy

ViajantesTurismo app APIs use URL path versioning. The current public contract version is `v1`, exposed under
`/api/v1/...`.

While the app remains alpha, `v1` identifies the active route shape; it does not promise that earlier
alpha contracts remain available.

The [generated endpoint route map](architecture/generated-endpoint-route-map.md) is the current
source-derived inventory of route prefixes and owning services. Do not duplicate that volatile list
here; committed OpenAPI artifacts remain the contract-review boundary.

Route versioning does not require every other request value to move into a body. Choose route, query,
header, form, body, and stream inputs by the [API request shape policy](API_REQUEST_SHAPES.md).

The unversioned API routes are not part of the supported HTTP contract. Browser-facing Management and Public Web routes
remain unversioned UI routes; typed API clients translate API calls to the versioned service endpoints.

OpenAPI artifacts keep the existing logical boundary files (`tours`, `customers`, `bookings`, `catalog`,
`public-catalog`, `branding`, and `public-branding`) plus their version-level review documents. A
shared process may generate more than one logical `v1` artifact: ADR-040 preserves Branding's
Branding-only canonical `v1` after Catalog.ApiService co-hosting rather than merging contract ownership.
Refresh committed artifacts only when the HTTP contract changes intentionally.

Conditional request semantics are part of the versioned HTTP contract. Branding management v1 uses
RFC 9110 strong ETag/`If-Match` comparison: valid false conditions return 412, missing required
preconditions return 428, and only malformed field syntax returns 400. Entity-tag lists, weak tags,
and `*` retain their standards-defined meanings; they are not alternate application-version signals.
Management `GET` returns the validator. A normalized successful `PUT` omits it and requires a follow-up
`GET`, because validators cannot describe a stored representation transformed from the request.

## Alpha evolution

While the app API is alpha, evolve its active route and implementation directly. Do not retain old
endpoint implementations, compatibility DTOs, or parallel `/api/v2` routes solely for backward
compatibility.

Remove replaced code and update affected callers, all affected tests including contract tests,
committed OpenAPI artifacts, and documentation in the same change. Document each significant
consumer-visible break and its migration impact.

This policy does not permit destructive data loss. Use a reviewed forward migration, preserve or
transform durable data before destructive removal, and keep the migration gate, backup, and recovery
practices in [production readiness](operations/production-readiness.md#backup-restore-and-disaster-recovery).
Alpha direct evolution does not require superseded application code or contract types to remain in the
active source tree.

## Deprecation from beta onward

The app API compatibility promise begins at beta. From beta onward, use `ApiVersionStatus.Deprecated`
for versions that remain selectable but should move consumers forward. Use `ApiDeprecationPolicy` for
sunset dates or migration information. Use `ApiVersionStatus.Retired` only when the version must no
longer be selected for requests.

Additive changes stay on the current version when they do not break existing clients. An incompatible
change from beta onward requires a new route segment such as `/api/v2`, with the older version deprecated
before retirement whenever practical.

## Value-object generation note

`ApiVersion` is value-object-shaped. It is implemented explicitly today so API versioning packages can ship without
waiting for the source-generation epic. Future work includes Vogen-like value-object generation and
specialized API version generation.
