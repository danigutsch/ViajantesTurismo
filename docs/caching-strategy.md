# Caching strategy

## Scope

Public content, Branding, and immutable public assets are cacheable under the policies below.
Tour visibility, management, Admin API, authenticated and PII-bearing reads, plus uploads, imports,
and mutations, use non-cacheable responses.

## Policies

| Surface | Policy | Freshness | Invalidation |
| --- | --- | --- | --- |
| Catalog API `/public/catalog/tours` and `/public/catalog/tours/{slug}` | `Cache-Control: no-store` | None | Reads current public visibility |
| Catalog API `/public/catalog/content/{key}` | Public HTTP metadata plus server output cache varied by canonical `culture` | 60 seconds plus `stale-while-revalidate=300` | Public content writes evict `public-content` |
| Branding public settings route (Catalog-hosted) | Public HTTP metadata plus host-local server output cache | 60 seconds plus `stale-while-revalidate=300` | Branding writes evict public branding cache entries in the active host |
| Branding management settings routes | No cache; `Cache-Control: no-store` | None | Not applicable |
| Public Web home, tour list and detail, gallery, sitemap, and Catalog rendition proxy | `Cache-Control: no-store` | None | Reads current public visibility |
| Public Web `/theme.css` | Public for anonymous successful reads | 60 seconds, immutable when the URL carries the content hash | A new content hash produces a new URL |
| Public Web redirects to a culture (`/`, `/gallery`) | Not public: they vary on `Accept-Language` and `Cookie` | None | Not applicable |

## Non-cacheable responses

- Management and Admin API responses are treated as editor or operator surfaces.
- Customer, booking, health diagnostics, imports, uploads, draft content, and authenticated responses are
  non-cacheable by default.
- Branding management reads and writes and all mutating Catalog responses return
  `Cache-Control: no-store`.

## Cache candidates and non-candidates

A review of every Management Web catalog read and every Admin `QueryService` read classified each as
cacheable or not from the code. Reads are cached only when the result is identical for all callers and a
stale value is harmless. Prefer pagination and projection over caching when a read is slow.

### Management Web catalog reads

| Read | Verdict | Reason |
| --- | --- | --- |
| Catalog tour list, tour edit, tour images, public content editor | Not cacheable | Editors save and immediately re-read; a stale list shows the wrong publish state or image readiness |
| Branding management settings | Not cacheable | `no-store` plus an ETag that is the `If-Match` concurrency token for saves |
| Country list (`CountryService`) | Cacheable, in process | Static reference data from `wwwroot/data/countries.json`; registered as a singleton so it loads once per process |
| Currency formatting | Nothing to cache | `CurrencyFormatter` is a static switch with no I/O |

Management Web registers the output cache, but no endpoint or page opts in, so it caches nothing today.

### Admin `QueryService` reads

The Admin tour, customer, and booking reads use current data.

| Read | Verdict | Reason |
| --- | --- | --- |
| `GetTourPage`, `GetTourById` | Not cacheable | Capacity depends on booking state. Document generation also reads tour details. |
| `GetCustomerPage`, `GetCustomerDetailsById` | Not cacheable | Both return personal data that must reflect current edits. Details include national ID and medical information. |
| `GetBookingPage`, `GetBookingById` | Not cacheable | Personal and payment ledger data. Document generation uses current booking values. |
| `GetTourBookingPage`, `GetCustomerBookingPage` | Not cacheable | Changes to bookings, travelers, or payments can affect history. |

The collection queries bound each read with paging. Tour and customer overview pages project summaries.

### Already cached

Anonymous public content and public Branding settings are output-cached for 60 seconds per instance,
with tag eviction from the mutating endpoints. Tour visibility reads bypass output caches so a
retirement acknowledgement can follow the durable withdrawal.

### FusionCache evaluation

Keep ASP.NET Core output caching for anonymous Catalog content and Branding. It coalesces
concurrent cold reads, supports tag eviction after writes, and keeps HTTP cache policy with
the route. The accepted freshness window covers the existing cross-instance delay.

| Option | Fit for this application |
| --- | --- |
| In-process data cache | Suits static reference data such as the country list. Each process owns its loaded copy. |
| Output caching and HTTP metadata | Fits the existing anonymous public reads and content-hashed assets. Endpoint tests check freshness, hit and miss outcomes, and invalidation. |
| `IDistributedCache` with Redis | Provides a shared key-value store when an application flow requires one. The AppHost already supports the Redis resource. Management authentication uses its dedicated protected PostgreSQL security store. |
| FusionCache with Redis | Combines local and distributed levels, coordinated invalidation through a backplane, and optional stale fail-safe reads. Evaluate it for a measured cross-instance data-cache need with explicit expiration and staleness budgets. |

Keep stale responses within their declared policy window. If the cache becomes unavailable,
recompute eligible reads from the owning store. The existing output-cache locking handles
concurrent recomputation for one key. Retain `no-store` for tour visibility and private or
authenticated responses.
[ASP.NET Core output caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output) ·
[Redis integration](https://aspire.dev/integrations/caching/redis/redis-get-started/) ·
[FusionCache cache levels](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/CacheLevels.md)

Revisit distributed invalidation after measurements show that the current multi-instance
freshness window or load coalescing cannot meet a named public-read target. Reuse the Aspire
Redis integration for that trial. Measure hit, miss, stale and invalidation outcomes without
resource identifiers. Existing Branding cache metrics and tests provide the comparison baseline.
[Branding cache telemetry](OBSERVABILITY_CATALOG.md) ·
[Platform integration strategy](architecture/platform-integrations.md)

## Eventual consistency

Public content and Branding mutations evict same-process output-cache entries before returning success.
Branding can be served stale while intermediaries revalidate during its documented window. For tour
withdrawal and the procedure for a future upgrade, see
[tour retirement and response caches](operations/public-visitor-deployment-validation.md#tour-retirement-and-response-caches).

The accepted Branding window does not justify Redis or another distributed cache. Add one only after
measured multi-instance invalidation, scale, or stricter freshness requirements show that host-local
output caching cannot meet the contract.

### Branding cache freshness

The public Branding cache is disposable and never authoritative; the committed Branding row, source
revision, and revision history are. The accepted bound is 60 seconds fresh plus 300 seconds stale.

- Eviction runs after the commit and is best-effort. A failed eviction is logged with the cache area and
    failure type only, and the committed `PUT` still returns success without a second revision.
- Each Catalog instance holds its own output cache, so a write evicts only the instance that handled
    it. Another instance can serve the previous settings until its entry expires, which the 60-second
    freshness window bounds. Persistence and Management reads stay current throughout.
- Concurrent cold public reads coalesce into a single load through ASP.NET Core output caching. No extra
    stampede protection is added.
- Restarting an instance empties its cache; the next read repopulates it from persistence.

Public cache staleness is separate from Admin projection lag. Admin document generation reads its
versioned Branding projection, which the integration-event worker updates, and never reads this cache.
Cache freshness is observed from the public response headers and eviction logs; projection lag is
observed from the Admin projection watermark and health.

## Observability and checks

Public content and Branding cache invalidation emit fixed-area logs for `public-content`
and `public-branding`. Tests cover public cache
metadata, output-cache hits, and update-then-read invalidation. Load checks should measure public list,
detail, content, and branding reads separately from management writes, and should avoid
high-cardinality dimensions such as tour slug or content key.
