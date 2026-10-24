# Public visitor deployment validation

This record validates forwarded visitor identity and visitor isolation across frontend replicas.
It distinguishes repository composition and in-process HTTP evidence from deployed evidence. No
production ingress addresses, networks, credentials, or hosting configuration are inferred here.

The local Aspire baseline was started from the reviewed worktree on 24 October 2026. Resource
diagnostics showed healthy PostgreSQL databases, Redis, scanner and object-storage resources; the
migration resource finished. The identity provider remained in `Starting`, while API and frontend
resources waited for dependencies. No end-to-end application health or ingress result is inferred
from that partial startup. The HTTP fixtures below provide the available executable evidence.

## Available topology evidence

The reviewed baseline is `3eb3732d68b2736775d16f2fb146e40993d6d00e` (24 October 2026).
The local Aspire model in `src/ViajantesTurismo.AppHost/AppHostResourceExtensions.cs` exposes Public Web
externally, references the internal Catalog and Branding services, and waits for their health.
`Aspire:Replicas:PublicWeb` defaults to one and accepts a positive replica count. Aspire endpoint
proxies balance local frontend instances; this setting does not select production ingress or provide
host failover. See [Public Web replicas](../../src/ViajantesTurismo.AppHost/README.md#public-web-replicas).

The [production readiness record](production-readiness.md#deployment-and-promotion-model) says the first
production target has not been selected. The repository contains no deployed ingress manifest or
environment-specific trusted-hop inventory. Release Prep exports an Aspire resource model rather than
deploying a compute environment. Therefore actual deployed peer addresses, ingress header behavior,
network isolation, replica routing, and effective runtime options remain unverified.

## Forwarding boundaries

Public Web and Catalog call `AddConfiguredTrustedForwardedHeaders` and execute `UseForwardedHeaders`
before their rate limiters. Public Web's Catalog HTTP client uses the existing resolved-client
forwarding handler. It overwrites outgoing `X-Forwarded-For` with the resolved remote address instead
of copying the incoming header chain. Catalog independently resolves that outgoing header against its
own trusted peer configuration.

The owning service binds `Security:ForwardedHeaders:KnownProxies`, `KnownNetworks`, and `ForwardLimit`.
No environment-specific trust entries are committed in the AppHost or application settings. The
default local composition relies on framework loopback trust. Explicit trust entries replace the
loopback defaults. A network entry alone does not establish forwarding depth; configure a positive
`ForwardLimit` explicitly for each service's observed inbound chain. Public Web's ingress chain and
Catalog's service-to-service chain require separate accounting.

Only proxy-controlled addresses or narrowly scoped proxy networks should enter the allowlist. Count
actual forwarding hops, including any endpoint proxy or service mesh that appends to the chain. Do not
derive the depth from the number of replicas or allowlisted addresses. An edge must remove or
overwrite untrusted incoming forwarding metadata before adding its own evidence; an application
allowlist cannot establish whether a deployed proxy does that.

## Local HTTP composition checks

`PublicCatalogDeploymentTrustTests` starts real Public Web and Catalog application pipelines with
test stores and TestServer transports. Three independent frontend hosts share one Catalog host. Its
addresses belong to documentation-only ranges and model explicit test topologies, not deployment
configuration recommendations.

The checks exercise:

- Two trusted ingress hops with `ForwardLimit = 2`, using both proxy-address and CIDR allowlists.
  Catalog separately trusts its test frontend peer with `ForwardLimit = 1`.
- A changing forged leftmost address that cannot mint new visitor allowances or consume an unrelated
  visitor's allowance. The CIDR case deliberately keeps additional candidate addresses inside the
  trusted range so the configured forwarding depth is observable.
- An untrusted intermediate hop that stops traversal even when the configured limit would allow
  another hop.
- One visitor exhausting the shared Catalog allowance through two frontends, then receiving the
  expected backend-quota outcome through a third frontend while another visitor retains access.
- Frontend admission exhaustion returning HTTP 429 on one frontend while another frontend still
  admits the same visitor, making the process-local admission boundary explicit.

The existing `PublicCatalogProxyRateLimitTests` additionally cover direct untrusted Catalog callers,
forged incoming forwarding chains, IPv4-mapped IPv6 identity, unaffected pages and sitemap, a normal
70-image gallery, uncached admission rejections, health and robots endpoints, and quota responses that
do not trigger shared retries or circuit-breaker failures. `ForwardedHeadersHostTests` covers HTTPS
scheme handling from trusted and untrusted peers. `CatalogApiSecurityBaselineTests` checks configured
trusted entries, forwarding limits, and invalid configuration rejection.

These fixtures provide local application-composition evidence. They do not run an actual ingress,
discover service mesh behavior, exercise real replica placement, or prove deployed isolation.

On 24 October 2026, the focused project build passed with zero warnings and errors. The four new
deployment-trust cases passed; the 29 existing proxy and Catalog baseline cases passed; the three
Public Web forwarding-host cases passed. Reproduce the focused runs with:

```bash
dotnet test --project tests/ViajantesTurismo.Catalog.ApiServiceTests/ViajantesTurismo.Catalog.ApiServiceTests.csproj \
    --filter-class ViajantesTurismo.Catalog.ApiServiceTests.PublicCatalogDeploymentTrustTests \
    ViajantesTurismo.Catalog.ApiServiceTests.PublicCatalogProxyRateLimitTests \
    ViajantesTurismo.Catalog.ApiServiceTests.CatalogApiSecurityBaselineTests --minimum-expected-tests 33
dotnet test --project tests/ViajantesTurismo.Public.WebTests/ViajantesTurismo.Public.WebTests.csproj \
    --filter-class ViajantesTurismo.Public.WebTests.ForwardedHeadersHostTests --minimum-expected-tests 3
```

## Process-local quotas and remaining deployment evidence

Public Web's admission limiter permits 120 requests per visitor per minute per frontend process.
Catalog's public-read limiter permits 240 requests per resolved visitor per minute per Catalog
process. The local multi-frontend checks share a single Catalog process; its visitor allowance
therefore spans those frontend instances. Restarting a process resets its in-memory allowance.
Replicating Catalog produces independent backend allowances; these checks do not establish a
deployment-wide quota across Catalog replicas. No new distributed limiter is introduced.

To complete an environment-specific validation, retain a private sanitized record of:

1. The actual request path from ingress through Public Web to Catalog, peer ownership, and which hop
   overwrites or appends each forwarding header.
2. Effective trusted proxy/network entries and explicit forwarding depth for both applications,
   checked against that path; backend reachability must be restricted to the intended callers.
3. Read-only spoofing controls and normal requests from independent test clients, with routing across
   each frontend replica recorded and quota outcomes correlated without retaining client identifiers.
4. Health, normal browsing, and unrelated HTTP resilience outcomes during the quota exercise, plus
   the Catalog replica count and resulting process-local limitation.

No deployment-specific acceptance is claimed until those observations are available. Account access,
deployment changes, and any infrastructure ownership decisions remain external operator actions.
