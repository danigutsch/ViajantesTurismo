# API Client Boundaries

This repository keeps API client contracts separate from app-local HTTP implementation details.

## Current app-local HTTP clients

Admin Management Web owns these typed HTTP client implementations:

- `BookingsApiClient`
- `CatalogToursApiClient`
- `CustomersApiClient`
- `PublicContentApiClient`
- `PublicThemeApiClient`
- `ToursApiClient`

Public Web owns this typed HTTP client implementation:

- `PublicCatalogApiClient`

Contract test projects also contain OpenAPI document clients that are test-local helpers, not app clients.

## Contract-owned rules

- Put shared `I*ApiClient` interfaces in the owning contract project when multiple apps or tests consume the seam.
- Put DTOs and response outcome shapes in the contract project when callers must handle stable API outcomes without
  knowing HTTP implementation details.
- Outcome shapes should model business-visible response branches such as success, validation problem, not found,
  unauthorized, forbidden, conflict, malformed body, empty body, and unexpected status when the endpoint can return them.
- Keep outcome shapes endpoint- or client-specific until at least two real clients share the same shape.

## App-local rules

- Keep concrete HTTP clients in the consuming app project.
- Keep raw `HttpClient` usage inside typed client implementations; component code should depend on `I*ApiClient`.
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
