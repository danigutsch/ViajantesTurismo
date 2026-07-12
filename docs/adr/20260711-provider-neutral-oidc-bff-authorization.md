# Provider-neutral OIDC BFF authorization

- Status: Accepted
- Date: 2026-07-11
- Epic: #805

## Context

Management browser sessions must not expose bearer or refresh tokens. Admin, Catalog, and
Branding APIs currently need a common, fail-closed authentication baseline while retaining the
explicit anonymous public catalog and branding reads. The selected production identity provider
must remain replaceable.

## Decision

- Use standards-based OIDC/OAuth 2.0. Management Web is a confidential authorization-code + PKCE
  client using a secure, `HttpOnly`, `SameSite=Lax`, `__Host-` cookie.
- APIs accept delegated bearer access tokens only. Each validates its configured issuer,
  discovery metadata, signature/JWKS, `RS256` algorithm, expiry, bounded clock skew, and exactly
  one audience: `admin-api`, `catalog-api`, or `branding-api`.
- Map the validated `roles` claim centrally into application-owned `permission` claims. Policies
  use permissions; they never authorize a raw provider role directly. `Admin` receives all
  boundary permissions. `Operator` receives the non-sensitive operational permissions assigned to
  its boundary.
- Apply an authenticated fallback policy. Only reviewed health probes, `robots.txt`, public Catalog
  reads, and public Branding reads are explicitly anonymous.
- Management Web stores protected cookie tickets, including saved OIDC tokens, in
  `security.management_cookie_tickets`. The ticket store uses a dedicated Data Protection purpose.
  Data Protection keys are shared through `security.data_protection_keys` and production startup
  requires `Authentication:DataProtection:CertificatePath` and
  `Authentication:DataProtection:CertificatePassword` to protect that key ring at rest.
- Management requests `offline_access`. Refresh-token rotation remains disabled until separate
  refresh-token persistence, advisory locking, post-lock reread, and compare-and-swap rotation are
  implemented; tokens remain in the protected server-side ticket. Track that distributed rotation
  work in #975.
- Management sign-in requests the approved Admin, Catalog, and Branding API scopes. Its protected
  server-side ticket can call those intended backends, while each typed client remains bound to its
  intended base address and public clients never receive bearer-token handlers. Token exchange
  remains disabled unless an identity provider explicitly supports RFC 8693 and the requested
  audience.
- Keycloak is a digest-pinned local and CI conformance identity provider only. Production identity
  configuration is provider-neutral and comes from deployment configuration or secret stores. The
  AppHost uses Aspire `ExecutionContext.IsRunMode` to declare Keycloak and inject its HTTP
  development authority; publish mode excludes the local identity-provider model entirely.

## Consequences

- Deployments must provision the PostgreSQL security schema, its least-privilege application role,
  Data Protection key encryption, expiry cleanup, and Keycloak/OIDC secrets outside source control.
- Authentication failures return `401`; valid identities without the required permission return
  `403`. Authorization failures do not disclose tokens, permissions, or customer data.
- OpenAPI documents describe the bearer scheme and attach `401`/`403` requirements only to
  protected operations.
- The remaining AppHost execution-context convention is tracked in #989.
