# Provider-neutral OIDC BFF authorization

- Status: Accepted
- Date: 2026-07-11

## Context

Management browser sessions must not expose bearer or refresh tokens. Admin, Catalog, and
Branding APIs currently need a common, fail-closed authentication baseline while retaining the
explicit anonymous public catalog and branding reads. The selected production identity provider
must remain replaceable.

## Decision

- Use standards-based OIDC/OAuth 2.0. Management Web is a confidential authorization-code + PKCE
  client using a secure, `HttpOnly`, `SameSite=Lax`, `__Host-` cookie.
- APIs accept delegated bearer access tokens only. Each validates its configured issuer,
  discovery metadata, signature/JWKS, `RS256` algorithm, expiry, bounded clock skew, and intended
  audience: `admin-api`, `catalog-api`, or `branding-api`. Management never forwards its source
  token to an API.
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
  implemented; tokens remain in the protected server-side ticket.
- Management sign-in requests the approved Admin, Catalog, and Branding API scopes. For each protected
  backend call, Management exchanges its protected server-side source access token through Keycloak
  RFC 8693 token exchange for the exact `admin-api`, `catalog-api`, or `branding-api` audience. The
  request supplies the source access token as `subject_token`, requests an access token, and uses the
  exact audience as both `audience` and scope. Each typed client sends only the exchanged audience
  token; neither source nor exchanged tokens reach the browser. Exchanged tokens are protected and
  cached server-side by source token and audience until shortly before expiry.
- OIDC authority, issuer, client ID, and client-secret configuration remain deployment-provided.
  The current Management BFF implementation requires
  `Authentication:TokenExchange:Enabled=true`,
  `Authentication:TokenExchange:Provider=Keycloak`, and a Keycloak-compatible RFC 8693 token
  endpoint. OIDC bearer validation, permission mapping, and API boundaries remain provider-neutral;
  supporting a different exchange provider requires an explicit implementation and decision.
- Keycloak is a digest-pinned local and CI conformance identity provider. The AppHost uses Aspire
  `ExecutionContext.IsRunMode` to declare Keycloak and inject its HTTP development authority; publish
  mode excludes the local identity-provider model entirely.

## Consequences

- Deployments must provision the PostgreSQL security schema, its least-privilege application role,
  Data Protection key encryption, expiry cleanup, Keycloak-compatible OIDC secrets, and the required
  token-exchange settings outside source control.
- Authentication failures return `401`; valid identities without the required permission return
  `403`. Authorization failures do not disclose tokens, permissions, or customer data.
- OpenAPI documents describe the bearer scheme and attach `401`/`403` requirements only to
  protected operations.
