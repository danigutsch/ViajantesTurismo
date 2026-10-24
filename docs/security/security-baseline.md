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

The [public visitor deployment validation record](../operations/public-visitor-deployment-validation.md)
documents local multi-frontend controls, process-local quota limits, and the remaining deployed
topology and effective-configuration evidence.

## Sensitive data logging

Catalog EF sensitive data logging is development-gated. Production and non-development environments must not
enable sensitive EF logging. Application logs must not include Customer PII, national ID, health details,
full CSV file contents, import rows, tokens, or credentials.

## Customer import

Customer import accepts small CSV files only. Empty files, oversized files, and non-CSV uploads fail with a
generic ProblemDetails response. Multipart request limits include the max CSV size, a conflict-resolution field
budget, and multipart envelope headroom. Malware scanning is considered separately.

## Recurring check

Run the pinned SharedKernel repository tool after candidate bootstrap and local tool restore:

```text
dotnet tool run sharedkernel-repo -- security baseline --root .
dotnet tool run sharedkernel-repo -- security scan --root . --output TestResults/security
```

The repository-owned `.config/security-baseline.json` declares required Markdown documents and text.
Documentation checks do not inspect source markers or prove runtime protection. Maintained security tests
verify headers, CORS, rate limits, import validation, protected routes, and environment-gated logging.
Dependency review and NuGet Audit retain their existing jobs and severity policy.

The scan command additionally runs Gitleaks on current files and writes redacted SARIF. The shared installer
pins Gitleaks `8.30.1` and verifies its archive against a committed SHA-256; this replaces the older `8.9.0`
binary and is a reviewed supply-chain change. Repository rules exclude generated build, package, and worktree
outputs while retaining source and tests. Git history scanning and Codex deep scans remain separate.

Exit codes: `0` success, `1` baseline violations or secret findings, `2` configuration/usage failure, and `3`
scanner execution/report failure or cancellation. Missing tools never pass. Raw scanner output is not
forwarded. Existing reports are not overwritten; retain previous evidence and use a fresh output directory.
PR, main, manual, and nightly security jobs run with read-only contents permission. SARIF publication remains
a separate write-scoped job restricted to trusted repository events. Fork and merge-queue scans do not
publish to code scanning with write credentials.
