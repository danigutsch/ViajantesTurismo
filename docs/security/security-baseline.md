# Security baseline

## Browser security headers

Public Web and Management Web emit:

- `Content-Security-Policy`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `X-Content-Type-Options: nosniff`
- `Permissions-Policy`

Management Web allows `connect-src 'self' ws: wss:` for Blazor Server SignalR. Public Web keeps `connect-src`
to `self`. Both browser clients reject inline styles and scripts by default.

## CORS

API CORS policies deny cross-origin browser access by default. Environment-specific allowed origins are read
from `Security:Cors:AllowedOrigins`. Local development can configure the Public Web and Management Web origins;
production should list only deployed HTTPS origins.

## Rate limiting

Named policies separate endpoint risk:

- `catalog-public-read`: public Catalog API reads, sized for normal browsing.
- `catalog-mutation`: Catalog management writes and AI draft generation.
- `admin-mutation`: Admin API mutation baseline.
- `admin-import`: customer CSV import and commit endpoints.

Remote-IP policies depend on trusted forwarded-header configuration when an API runs behind a reverse proxy or
load balancer. Configure `Security:ForwardedHeaders:KnownProxies` or
`Security:ForwardedHeaders:KnownNetworks` for the trusted proxy hops before enabling production traffic through
that proxy; otherwise clients can share the proxy IP rate-limit bucket. Set
`Security:ForwardedHeaders:ForwardLimit` explicitly from the expected `X-Forwarded-For` proxy hop count when
traffic crosses multiple proxies; CIDR network entries do not imply a hop count.

## Sensitive data logging

Catalog EF sensitive data logging is development-gated. Production and non-development environments must not
enable sensitive EF logging. Application logs must not include Customer PII, national ID, health details,
full CSV file contents, import rows, tokens, or credentials.

## Customer import

Customer import accepts small CSV files only. Empty files, oversized files, and non-CSV uploads fail with a
generic ProblemDetails response. Multipart request limits include the max CSV size, a conflict-resolution field
budget, and multipart envelope headroom. Malware scanning is considered separately.

## Recurring check

Run the .NET security baseline tool:

```text
dotnet run --project tools/ViajantesTurismo.SecurityChecks.Tool -- baseline .
```

The tool checks required threat-model docs, control docs, security-header code, CORS/rate-limit wiring, import
validation, and development-gated sensitive data logging. It does not replace dependency review or NuGet audit.
