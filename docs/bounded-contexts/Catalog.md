# Catalog Bounded Context

The Catalog bounded context owns customer-facing tour presentation for public discovery and public
website experiences.

## Context Overview

**Domain:** Customer-facing tour catalog and publication
**Teams:** Marketing, operations, content editing
**Primary Users:** Website visitors, customers, content editors

Catalog is downstream of Admin for selected operational tour facts, but it owns its own public
presentation model.

## Responsibilities

- **Published tour presentation** - Maintain customer-facing tour titles, summaries, descriptions,
  hero images, galleries, itinerary content, SEO metadata, and call-to-action links.
- **Publication workflow** - Track draft, published, unpublished, and archived customer-facing
  states.
- **Public read models** - Provide optimized projections for public listing and detail pages.
- **Management editing** - Provide management-facing APIs for editing public presentation content.
- **Versioned content history** - Store customer-facing changes as event streams for replay,
  diagnostics, and projection rebuilds.

## Bounded Context Map

### Upstream Dependencies

- **Admin** - Publishes integration events for selected operational tour changes such as tour
  creation, schedule updates, and archival.

### Downstream Consumers

- **Public.Web** - Reads published Catalog projections for the customer website.
- **Management.Web** - Edits Catalog presentation fields through typed Catalog clients.
- **Search/Marketing integrations** (future) - May consume Catalog projections or integration
  events.

## Relationship to Admin

Admin owns operational tour management, booking lifecycle, customer records, pricing operations,
capacity, and payment workflows. Catalog owns customer-visible presentation of tours.

Admin-to-Catalog synchronization uses explicit integration events. Catalog must not reference Admin
implementation projects or reuse the Admin `Tour` aggregate as its public website model.

Example flow:

```text
Admin creates a Tour
Admin publishes AdminTourCreatedIntegrationEvent
Catalog consumes the event
Catalog creates a draft CatalogTour event stream
Management.Web edits customer-facing content
Catalog publishes projections
Public.Web renders published tours
```

## Aggregate Model

### CatalogTour

**Purpose**: Own the customer-facing versioned presentation of one tour.

Expected root:

- `CatalogTour : EventSourcedAggregateRoot<CatalogTourId>`

Expected data:

- Admin tour id reference.
- Identifier copied from Admin.
- Slug.
- Title.
- Summary.
- Description.
- Hero image URL.
- Gallery items.
- Itinerary content.
- SEO title and description.
- Publication status.
- Display date information.
- Customer-facing call-to-action links.

Expected invariants:

- Slug must be non-empty, URL-safe, and unique within Catalog.
- Published tours require title, summary, slug, and minimum public content.
- Archived tours cannot be published without explicit reactivation.
- Public detail projections are built from published Catalog state only.

## Event Sourcing

Catalog tours use append-only event streams.

Initial domain events:

- `CatalogTourDraftCreated`.
- `CatalogTourTitleChanged`.
- `CatalogTourSummaryChanged`.
- `CatalogTourHeroImageChanged`.
- `CatalogTourItineraryChanged`.
- `CatalogTourGalleryChanged`.
- `CatalogTourSeoMetadataChanged`.
- `CatalogTourPublished`.
- `CatalogTourUnpublished`.
- `CatalogTourArchived`.

Projection types:

- Management editor read model.
- Public tour listing read model.
- Public tour detail read model.
- Optional search/filter read model.

Projection consistency:

- Use inline projections when the read model must update with the same transaction as the event
  append.
- Use asynchronous projections later for heavier public/search/gallery views.

## Integration Events Consumed

Initial Admin events consumed by Catalog:

- `AdminTourCreatedIntegrationEvent`.
- `AdminTourDetailsChangedIntegrationEvent`.
- `AdminTourScheduleChangedIntegrationEvent`.
- `AdminTourArchivedIntegrationEvent`.

Catalog consumers must be idempotent. The Catalog inbox records processed integration event ids so
at-least-once delivery can safely retry.

## Persistence

Catalog may share the same physical PostgreSQL resource as Admin initially, but it owns separate
schema and tables.

Expected tables:

- `catalog.event_streams`.
- `catalog.events`.
- `catalog.projection_checkpoints`.
- `catalog.integration_inbox`.
- `catalog.integration_outbox` if Catalog publishes integration events later.
- Catalog read-model tables for management and public projections.

## API Surface

Management-facing endpoints should edit customer-facing presentation data only.

Initial management endpoints:

- `GET /catalog/tours`.
- `GET /catalog/tours/{id}`.
- `PUT /catalog/tours/{id}/presentation`.
- `POST /catalog/tours/{id}/publish`.
- `POST /catalog/tours/{id}/unpublish`.

Public-facing endpoints should return published projections only.

Initial public endpoints:

- `GET /public/catalog/tours`.
- `GET /public/catalog/tours/{slug}`.

## Related Documentation

- [ADR-021: Catalog Bounded Context for Public Tour Presentation](../adr/20260621-catalog-bounded-context-for-public-tour-presentation.md)
- [ADR-025: Event Source Catalog Tour Presentation](../adr/20260621-event-source-catalog-tour-presentation.md)
- [ADR-020: Web Frontends by Audience, Not by Bounded Context](../adr/20260523-web-frontends-by-audience-not-by-bounded-context.md)
- [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
