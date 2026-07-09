# Caching strategy

## Scope

Public catalog reads and public web server-side rendered pages are cacheable. Management, Admin API,
authenticated, PII-bearing, upload, import, and mutation responses are non-cacheable unless a later issue
documents a narrower safe exception.

## Policies

| Surface | Policy | Freshness | Invalidation |
| --- | --- | --- | --- |
| Catalog API `/public/catalog/tours` and `/public/catalog/tours/{slug}` | Public HTTP metadata plus server output cache | 60 seconds | Catalog presentation and media writes evict `public-catalog` |
| Catalog API `/public/catalog/content/{key}` | Public HTTP metadata plus server output cache varied by `culture` and `language` | 60 seconds | Public content writes evict `public-content` |
| Catalog API `/public/catalog/theme` | Public HTTP metadata plus server output cache | 60 seconds | Theme writes evict `public-theme` |
| Public Web `/`, `/group-bike-tours`, `/group-bike-tours/{slug}`, `/gallery` | Public HTTP metadata plus server output cache varied by `culture` and `language` | 60 seconds | Expires after freshness window; Catalog API invalidation is service-local |

## Non-cacheable responses

- Management and Admin API responses are treated as editor or operator surfaces.
- Customer, booking, health diagnostics, imports, uploads, draft content, and authenticated responses are
  non-cacheable by default.
- Mutating Catalog API responses return `Cache-Control: no-store`.

## Eventual consistency

Catalog API mutations evict same-process public API output-cache entries before returning success. Public
Web pages cache rendered HTML in the Public Web process, so catalog changes can remain visible there until
the 60-second freshness window expires.

## Observability and checks

Catalog cache invalidation emits fixed-area logs: `public-catalog`, `public-content`, and `public-theme`.
Tests cover public cache metadata, output-cache hits, and update-then-read invalidation. Load checks should
measure public list, detail, content, and theme reads separately from management writes, and should avoid
high-cardinality dimensions such as tour slug or content key.
