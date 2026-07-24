# Caching strategy

## Scope

Public catalog reads and public web server-side rendered pages are cacheable. Management, Admin API,
authenticated, PII-bearing, upload, import, and mutation responses are non-cacheable unless a later issue
documents a narrower safe exception.

## Policies

| Surface | Policy | Freshness | Invalidation |
| --- | --- | --- | --- |
| Catalog API `/public/catalog/tours` and `/public/catalog/tours/{slug}` | Public HTTP metadata plus server output cache | 60 seconds plus `stale-while-revalidate=300` | Catalog presentation and media writes evict `public-catalog` |
| Catalog API `/public/catalog/content/{key}` | Public HTTP metadata plus server output cache varied by canonical `culture` | 60 seconds plus `stale-while-revalidate=300` | Public content writes evict `public-content` |
| Branding public settings route (standalone currently; Catalog-hosted target) | Public HTTP metadata plus host-local server output cache | 60 seconds plus `stale-while-revalidate=300` | Branding writes evict public branding cache entries in the active host |
| Branding management settings routes | No cache; `Cache-Control: no-store` | None | Not applicable |
| Public Web `/`, `/group-bike-tours`, `/group-bike-tours/{slug}`, `/gallery` | Public HTTP metadata varied by canonical `culture` | 60 seconds plus `stale-while-revalidate=300` | Server output cache is not used because Catalog API invalidation is service-local |

## Non-cacheable responses

- Management and Admin API responses are treated as editor or operator surfaces.
- Customer, booking, health diagnostics, imports, uploads, draft content, and authenticated responses are
  non-cacheable by default.
- Branding management reads and writes and all mutating Catalog responses return
  `Cache-Control: no-store`.

## Eventual consistency

Catalog and Branding mutations evict same-process public API output-cache entries before returning
success. This remains host-local when Branding moves into Catalog.ApiService. Public Web pages cache
rendered HTML in the Public Web process, so catalog or branding changes can remain visible there until
the 60-second freshness window expires. Intermediaries may serve stale public responses for up to
another 300 seconds while they revalidate.

The accepted Branding window does not justify Redis or another distributed cache. Add one only after
measured multi-instance invalidation, scale, or stricter freshness requirements show that host-local
output caching cannot meet the contract.

## Observability and checks

Catalog cache invalidation emits fixed-area logs: `public-catalog` and `public-content`. Branding
cache invalidation should use a separate fixed area such as `public-branding`. Tests cover public cache
metadata, output-cache hits, and update-then-read invalidation. Load checks should measure public list,
detail, content, and branding reads separately from management writes, and should avoid
high-cardinality dimensions such as tour slug or content key.
