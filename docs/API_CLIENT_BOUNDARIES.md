# API Client Boundaries

This repository keeps shared API client interfaces and HTTP implementations in the owning contract
project. Apps configure base addresses and keep UI fallback behavior local.

## Current contract-owned HTTP clients

`ViajantesTurismo.Admin.Contracts` owns these typed HTTP client implementations:

- `BookingsApiClient`
- `CustomersApiClient`
- `ToursApiClient`

`ViajantesTurismo.Catalog.Contracts` owns these typed HTTP client implementations:

- `CatalogToursApiClient`
- `PublicContentApiClient`
- `PublicCatalogApiClient`
- `PublicThemeApiClient`

Contract test projects also contain OpenAPI document clients that are test-local helpers, not app clients.

## Contract-owned rules

- Put shared `I*ApiClient` interfaces and implementations in the owning contract project when apps or tests consume the seam.
- Put DTOs and response outcome shapes in the contract project when callers must handle stable API outcomes without
  knowing HTTP implementation details.
- Keep contract clients AOT-compatible. Use per-client `JsonSerializerContext` types and pass generated
  `JsonTypeInfo<T>` metadata to HTTP JSON calls.
- Use the shared `ContractValidationProblemDto` shape for validation problem response parsing. Do not depend on
  ASP.NET Core `ProblemDetails` or `ValidationProblemDetails` from contract projects.
- Outcome shapes should model business-visible response branches such as success, validation problem, not found,
  unauthorized, forbidden, conflict, malformed body, empty body, and unexpected status when the endpoint can return them.
- Keep outcome shapes endpoint- or client-specific until at least two real clients share the same shape.
- Add logging or activity tags in contract clients when outcomes expose non-success branches that callers need to
  diagnose.

## App-local rules

- Configure contract-owned typed HTTP clients in the consuming app project with the app-specific base address.
- Keep raw `HttpClient` usage inside contract-owned typed client implementations; component code should depend on
  `I*ApiClient`.
- Keep app fallback behavior outside contract clients. For example, a missing `Location` header can be returned as a
  successful outcome without a location; the app decides whether to navigate to a safe fallback route.
- Do not introduce shared helper abstractions until at least two current clients use the same response shape.

## Proven outcome model

`CustomersApiClient.CreateCustomer` proves the first contract-owned outcome shape:

- `CustomerCreateOutcomeDto` carries `Kind`, `StatusCode`, optional `Location`, optional `ValidationErrors`, and an optional
  diagnostic `Message`.
- `CustomerCreateOutcomeKind` models success, validation problem, not found, unauthorized, forbidden, conflict, empty
  body, malformed body, and unexpected status.
- The Web app maps non-success outcomes to user-focused UI errors and keeps navigation fallback local to the component.
