# ADR-031: Contract-Owned API Clients

## Context

Management Web and Public Web previously owned typed HTTP client implementations for Admin and
Catalog endpoints. The interfaces and DTOs lived in contract projects for several seams, but the
HTTP parsing, validation problem handling, and response branch behavior lived in app projects.

That split made tests and apps depend on app-local client behavior even when the behavior was part of
the API contract. It also made it easier for each app to parse validation problems differently.

The contract projects are AOT-compatible, so they must avoid reflection-based JSON and ASP.NET Core
server-boundary dependencies.

## Decision

Shared typed API clients belong in the owning contract project with their `I*ApiClient` interface,
DTOs, response outcome DTOs, and response parsing behavior.

- Admin API clients live in `ViajantesTurismo.Admin.Contracts`.
- Catalog API clients live in `ViajantesTurismo.Catalog.Contracts`.
- Apps configure base addresses and consume the contract-owned interfaces.
- UI fallback behavior remains app-local.
- Contract clients use per-client `JsonSerializerContext` types and generated `JsonTypeInfo<T>`
  metadata for AOT-safe JSON.
- Contract clients use `ContractValidationProblemDto` for validation problem parsing instead of
  ASP.NET Core `ProblemDetails` or `ValidationProblemDetails`.

## Consequences

### Positive

- Apps and tests share one client implementation per API seam.
- Validation problem parsing is standardized without taking an ASP.NET dependency in contracts.
- Contract clients remain AOT-compatible.
- Focused HTTP seam tests can exercise the same client implementation apps use.

### Negative

- Contract projects now reference HTTP client and logging abstractions.
- Each client needs a small source-generated JSON context.

## Alternatives

### Keep concrete clients app-local

Rejected. It keeps duplicate response parsing outside the contract boundary and weakens seam tests.

### Use ASP.NET Core `ProblemDetails` and `ValidationProblemDetails`

Rejected. Those types are server-boundary conveniences and would couple AOT-compatible contract
projects to ASP.NET Core MVC abstractions.

### Create a broad shared HTTP result client library now

Rejected for this PR. Only one endpoint currently proves a full outcome model. A shared library can
be revisited after at least two clients share the same response shape.
