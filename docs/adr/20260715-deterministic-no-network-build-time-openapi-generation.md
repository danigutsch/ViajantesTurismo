# ADR-037: Deterministic, No-Network Build-Time OpenAPI Generation

## Context

HTTP contract artifacts must remain reproducible after runtime APIs adopted fail-closed bearer
authentication. The supported ASP.NET Core generator launches the application entry point, so ordinary
startup registration can otherwise load credentials, configure telemetry, register external integrations,
or perform OpenID Connect discovery.

Safe generation of logical Admin, Catalog, and Branding documents requires a durable architecture
decision even when one executable hosts more than one logical boundary.

## Decision

Use the first-party `Microsoft.Extensions.ApiDescription.Server` MSBuild generator. The repository-owned
OpenAPI tool is only a deterministic process boundary: it invokes `dotnet build --no-restore` with an
explicit, minimal child environment, disables CLI and OpenTelemetry exporters, and does not forward host
authentication, connection-string, proxy, or arbitrary environment values.

`SharedKernel.OpenApi.OpenApiGenerationMode` enables generation behavior only when both conditions hold:

1. the host environment is `OpenApiGeneration`; and
2. the entry assembly is `GetDocument.Insider`.

Shared registration methods use that trusted mode implicitly. Non-security registrations retain nullable
overrides for focused tests or exceptional host composition; authentication and authorization have no
public bypass. In trusted generation, APIs retain authorization policies and endpoint metadata but omit
JWT bearer registration, claims transformation, authentication middleware, authorization middleware,
telemetry, service discovery, and runtime worker relays.

`BearerSecurityDocumentTransformer` continues to describe bearer security, `401`, and `403` responses
from endpoint authorization metadata and the authenticated fallback policy; it does not require a JWT
handler or identity-provider configuration.

OpenAPI tool targets are logical contract owners, not permanent executable owners. The stable
`generate branding` command currently builds `ViajantesTurismo.Branding.ApiService`; before ADR-040
removes that executable, the command must build Catalog.ApiService in a Branding-only generation mode.
That mode preserves canonical `branding`, `public-branding`, and Branding-only `v1` artifacts under
`ViajantesTurismo.Branding.Contracts.Http/OpenApi`, including their existing refresh command and
Branding contract-test ownership. Catalog-hosted generation must not merge Branding artifacts into
Catalog ownership or include unrelated Catalog routes in the Branding-only documents.

Branding generation also preserves the Management conditional contract: strong `ETag` on `GET`,
required `If-Match` on `PUT`, no successful-`PUT` validator after normalization, RFC 9110 entity-tag
list/weak-tag/`*` semantics, and distinct 400 malformed-syntax, 412 false-precondition, and 428
missing-precondition responses.

The contraction gate updates tool target tests, generated filename mapping, drift guards, refresh
hints, and contract tests before deleting the standalone project. Trusted generation still omits
runtime relays, database access, service discovery, and OIDC/JWT registration while retaining both
named route schemes' authorization metadata.

## Consequences

### Positive

- Generated documents require no authority, issuer, client secret, user secret, external IdP, or network
  restore.
- Normal API startup remains fail-closed: a configuration marker or environment name alone cannot omit
  bearer authentication because it cannot satisfy the `GetDocument.Insider` identity check.
- Contract drift tests consume the same generated Admin, Catalog, and Branding artifacts that CI creates.
- Branding artifact ownership survives process co-hosting; executable removal cannot silently delete or
  merge the logical contract boundary.
- Runtime OpenAPI inspection remains development-only; artifact production stays build-time and explicit.

### Negative

- The tool must preserve a small platform-specific process environment so MSBuild can invoke `dotnet` and
  its shell host.
- The generator still executes endpoint mapping, so new startup side effects must be safe under the
  trusted generation mode.
- OS or CI egress controls remain the final enforcement layer for a hard network prohibition.

## Alternatives

### Configuration marker alone

Rejected. `OpenApi:BuildGeneration=true` can be supplied by a normal deployment and would make
authentication behavior deployment-configurable.

### Static or fake OIDC configuration

Rejected. It keeps generation coupled to authentication implementation details and adds token-validation
or identity-provider assumptions that OpenAPI document construction does not need.

### Runtime `MapOpenApi()` export

Rejected for contract artifacts. It needs a running HTTP host, readiness handling, endpoint exposure, and
an explicit authorization decision, making CI output less deterministic.

## Status

Accepted.

## Links

- [Architecture decision index](../ARCHITECTURE_DECISIONS.md)
- [API compatibility workflow](../API_COMPATIBILITY.md)
- [ADR-040: Branding Module Co-hosting and State Distribution](20260724-branding-module-co-hosting-and-state-distribution.md)
- [ASP.NET Core build-time OpenAPI generation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
