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
- A contract project owns a client only for the API surface it also owns. Cross-context or cross-app convenience clients stay in
  the consuming app until the API contract owner accepts the seam.
- Keep one interface per cohesive API seam. Do not add broad gateway or facade clients just to group unrelated endpoints.
- Put DTOs and response outcome shapes in the contract project when callers must handle stable API outcomes without
  knowing HTTP implementation details.
- Keep contract clients AOT-compatible. Use per-client `JsonSerializerContext` types and pass generated
  `JsonTypeInfo<T>` metadata to HTTP JSON calls.
- Use source-generation-backed JSON for enum serialization too. Prefer `JsonStringEnumConverter<TEnum>` or
  `JsonSourceGenerationOptionsAttribute.UseStringEnumConverter`; do not add the reflection-based non-generic
  `JsonStringEnumConverter` in Native AOT-compatible contract projects.
- Use the shared `ContractValidationProblemDto` shape for validation problem response parsing. Do not depend on
  ASP.NET Core `ProblemDetails` or `ValidationProblemDetails` from contract projects.
- Outcome shapes should model caller-visible response branches such as success, validation problem, not found,
  unauthorized, forbidden, conflict, malformed body, empty body, and unexpected status when the endpoint can return them.
- Keep outcome shapes endpoint- or client-specific until at least two real clients share the same shape.
- Add logging or activity tags in contract clients when outcomes expose non-success branches that callers need to
  diagnose.
- Never log response bodies, request bodies, headers, customer fields, validation error text, file names, or other PII from
  contract clients. Log bounded route-independent fields only.

## App-local rules

- Configure contract-owned typed HTTP clients in the consuming app project with the app-specific base address.
- Treat factory-created typed clients as short-lived services. Do not inject typed clients into singleton services; use a
  named client or a long-lived `HttpClient` with `SocketsHttpHandler.PooledConnectionLifetime` when singleton usage is needed.
- Avoid `IHttpClientFactory` for cookie-dependent clients unless handler isolation is explicitly designed. Handler pooling can
  share `CookieContainer` instances and handler recycling can drop cookies.
- Configure bounded outbound concurrency, such as `MaxConnectionsPerServer` or HTTP/2, when a client can issue many parallel
  HTTP/1.1 requests.
- Keep raw `HttpClient` usage inside contract-owned typed client implementations; component code should depend on
  `I*ApiClient`.
- Keep app fallback behavior outside contract clients. For example, a missing `Location` header can be returned as a
  successful outcome without a location; the app decides whether to navigate to a safe fallback route.
- Keep UI wording, retry prompts, navigation decisions, and feature-specific fallback state in the consuming app.
- Do not introduce shared helper abstractions until at least two current clients use the same response shape.

## Ownership checklist

Use this checklist before adding or moving an API client into a contract project:

- The owning contract project also owns the route, DTO, or published API contract being called.
- At least one app or test consumes the seam through an `I*ApiClient` interface.
- The client behavior is part of the contract boundary, such as validation-problem parsing or documented response branches.
- The client can stay AOT-compatible without ASP.NET Core MVC dependencies.
- App-specific base addresses, resilience policy selection, UI fallback, and user messaging remain outside the contract project.

If any item is false, keep the client app-local until the contract boundary is clearer.

## Proven outcome model

`CustomersApiClient.CreateCustomer` proves the first contract-owned command outcome shape:

- `ContractCommandOutcomeDto` carries `Kind`, `StatusCode`, optional `Location`, optional `ValidationErrors`, and an optional
  diagnostic `Message`.
- `ContractCommandOutcomeKind` models success, validation problem, not found, unauthorized, forbidden, conflict, empty
  body, malformed body, and unexpected status.
- The Web app maps non-success outcomes to user-focused UI errors and keeps navigation fallback local to the component.

## Response outcome modeling

Use endpoint-specific outcome DTOs when callers need to branch on expected HTTP responses without learning raw HTTP parsing.
Outcome DTOs should:

- expose a small `Kind` enum for documented branches;
- include `StatusCode` when diagnostics, logs, or callers need the raw HTTP status;
- include stable contract data such as `Location` or validation errors only when the caller uses it;
- include a short diagnostic `Message` only for non-PII parser state, such as empty or malformed response body;
- avoid exposing raw response content, headers, route values, or transport implementation details.

Use exceptions for cancellation, programmer errors, and transport failures until a real caller needs those failures as stable
contract outcomes. Do not replace every HTTP error with a generic result wrapper by default.

## Result-based client design note

Official .NET guidance supports typed clients registered through `IHttpClientFactory` as a DI-friendly place to configure
and interact with a backend. It also recommends either short-lived factory-created clients or long-lived clients with
`PooledConnectionLifetime` to avoid socket exhaustion and stale DNS. Contract clients should stay typed-client based and
let apps configure base addresses and resilience.

For AOT-safe JSON, use `System.Net.Http.Json` overloads that accept `JsonTypeInfo<T>` or `JsonSerializerContext`. The
reflection-based overloads carry trimming/dynamic-code warnings and do not belong in contract projects.

Recommended outcome shape:

- Return endpoint-specific outcome DTOs when callers must branch on expected HTTP responses, empty bodies, malformed JSON,
  validation problems, or fallback paths.
- Keep cancellation and programmer errors as exceptions.
- Keep transport exceptions as exceptions until a real caller needs an explicit transport outcome.
- Put reusable outbound HTTP defaults in `SharedKernel.HttpClients`, starting with service-discovery and resilience defaults.
- Apps/projects that perform outbound HTTP calls reference `SharedKernel.HttpClients` directly and call `AddHttpClientDefaults()`.
- `ViajantesTurismo.ServiceDefaults` keeps host-wide service discovery and telemetry, but does not configure outbound
  HTTP clients for projects that do not make HTTP calls.
- Add shared result/outcome adapters in `SharedKernel.HttpClients` or a focused `SharedKernel.Results.Http` package when the
  migration needs reusable parsing behavior across contract clients.

Diagnostics split:

- Contract clients log safe, structured fields for non-success outcome branches: API area, operation, status code, and
  outcome kind.
- Callers log user-flow context only when they add useful non-PII context.
- Contract clients do not log payloads, validation messages, or customer identifiers.

## Telemetry and logging expectations

Contract clients should emit telemetry only where it helps diagnose a stable contract outcome. Keep the signal bounded:

- activity source names belong to the owning contract assembly;
- activity names use stable operation names, not route templates or user-facing labels;
- tags use low-cardinality values such as API area, operation, HTTP method, HTTP status code, and outcome kind;
- warning logs are reserved for non-success branches returned as outcomes;
- callers add app-flow context only when it is non-PII and not already present on the contract outcome.

Never log or tag request bodies, response bodies, headers, bearer tokens, cookies, route parameters, customer fields,
validation message text, file names, or free-form user input. Prefer absence over redaction when the field is not required for
diagnosis.

Keep default HTTP instrumentation URL and header redaction enabled. Do not disable query-value redaction or opt into captured
request/response headers without a privacy review. If a handler needs scoped credentials or request context, keep that logic in
a transient delegating handler and do not cache `HttpContext`, scoped services, tokens, or user data inside pooled
`HttpMessageHandler` instances.

References:

- [Use the IHttpClientFactory - .NET](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
- [HttpClient guidelines for .NET](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Common `IHttpClientFactory` usage issues](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory-troubleshooting)
- [HttpContentJsonExtensions.ReadFromJsonAsync](https://learn.microsoft.com/dotnet/api/system.net.http.json.httpcontentjsonextensions.readfromjsonasync)
- [How to use source generation in System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation)
- [Semantic conventions for HTTP spans](https://opentelemetry.io/docs/specs/semconv/http/http-spans/)
- [Compile-time logging source generation - .NET](https://learn.microsoft.com/dotnet/core/extensions/logging/source-generation)

## Contract client diagnostics fields

`CustomersApiClient.CreateCustomer` emits one safe diagnostics slice for non-success create outcomes:

- activity source: `ViajantesTurismo.Admin.Contracts.Clients`
- activity name: `customers.create`
- activity kind: `Client`
- tags: `viajantes.api_area`, `viajantes.operation`, `http.response.status_code`,
  `viajantes.customer_create.outcome`
- warning log fields: `StatusCode`, `OutcomeKind`

The log and tags intentionally omit response bodies, request bodies, headers, validation error text, customer fields, file
names, and route parameters.

## Contract client testing guidance

Test contract-owned clients at the HTTP seam they own:

- use fake `HttpMessageHandler` instances or in-memory test servers to return documented status codes and bodies;
- verify AOT-safe serialization paths by exercising the client implementation, not by duplicating JSON parsing in fakes;
- cover each documented outcome branch that callers rely on;
- verify safe diagnostics fields for non-success outcomes when logging or activities are part of the contract behavior;
- keep UI fallback, navigation, and user-message assertions in app or component tests.

Avoid fakes that reimplement HTTP parsing. App tests should fake the `I*ApiClient` interface by returning documented outcomes.
