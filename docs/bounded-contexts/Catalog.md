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
- **Editable public website content** - Own business-editable website text and SEO content for
  English and Brazilian Portuguese variants.

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

Current runtime notes:

- `AdminTourCreatedIntegrationEvent` exists and Admin dispatches it in-process after tour creation.
- Catalog has a tested consumer that can append a `CatalogTourDraftCreated` event.
- The durable outbox/transport/inbox path between running Admin.ApiService and Catalog.ApiService is
  still planned/evolving.

See [Architecture flows](../architecture/FLOWS.md#catalog-event-sourcing-and-projection-flows)
for current and planned event-sourcing diagrams.

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

Current implementation:

- `CatalogTour` is an event-sourced aggregate that currently creates and applies
  `CatalogTourDraftCreated` only.
- The current management presentation endpoint updates `CatalogTourReadModels` directly; presentation
  edit events are planned/evolving.
- `CatalogTourDto.Images` is populated from Catalog media metadata; public endpoints include only
  images whose processing status is ready.

### Slug Policy

Catalog owns public tour slugs because they are part of the customer-facing URL contract. Slug
generation and validation should stay in Catalog domain/application code until another maintained
bounded context needs the same rules.

Initial slug rules:

- Slugs are required for published tours.
- Slugs are lowercase ASCII path segments using `a-z`, `0-9`, and single hyphens.
- Whitespace and separator runs collapse to one hyphen.
- Leading and trailing hyphens are removed.
- Accented Latin letters normalize to their ASCII base letter when practical.
- Slugs must not exceed the Catalog contract maximum length.
- Slugs are unique within Catalog published and draft tour records.
- Slugs should be stable after publication; changes need explicit redirect/compatibility handling.

Keep conventional UI labels and unrelated URL helpers out of this model. If future CMS or media
features need identical URL-safe identifier rules, create a focused SharedKernel extraction issue
after the second real caller exists.

### EditablePublicContent

**Purpose**: Own business-editable public website text that is not a conventional UI label.

Expected data:

- Stable content key, such as a page or section identifier.
- Source language entered by the editor.
- English (`en-US`) content variant.
- Brazilian Portuguese (`pt-BR`) content variant.
- Publication state: draft, review required, or published.
- SEO title, meta description, and social sharing summary where those are business content.

Initial rules:

- Both supported language variants are modeled explicitly.
- The English slot must contain `en-US`, and the Brazilian Portuguese slot must contain `pt-BR`.
- The editor source language must be one of the supported languages.
- AI-generated or machine-translated variants are marked as requiring human review.
- Content with any review-required variant starts in review-required state.
- Content can move to published only after no variant requires human review.
- Published rendering must use published content only.
- Conventional labels such as About and Gallery stay in code or localization resources unless a
  business-editing need appears.

The initial domain model is `EditablePublicContent` with `PublicContentVariant` values. Management
editor persistence, management API routes, and published-only public read routes are implemented.

Current implementation:

- `EditablePublicContent` and `PublicContentVariant` are implemented in Catalog domain.
- `PublicContent` and `PublicContentVariants` are persisted by Catalog infrastructure.
- `GET /catalog/public-content`, `GET /catalog/public-content/{**key}`, and
  `PUT /catalog/public-content/{**key}` are implemented as management-facing routes.
- `GET /public/catalog/content/{**key}` is implemented as a published-only public route with approved
  language-variant selection.
- Explicit review approval and manual publication endpoints are still planned/evolving.

See [Architecture flows](../architecture/FLOWS.md#public-content-localization-and-review-flows)
for the localization and review flow.

### Media and Gallery Metadata

Current implementation:

- `CatalogTourImageDto` defines public image metadata with required `Uri`, required `AltText`,
  optional `Caption`, responsive variants, ordering, and cover-image data.
- `CatalogTourDto.Images` is populated on management and public tour responses.
- `GET /catalog/tours/{id}/images` and `PUT /catalog/media/images/{id}` persist Catalog-owned image
  metadata and tour associations.

Planned/evolving:

- Gallery changes should move behind event-sourced Catalog commands before image read models are
  treated as rebuildable projections.
- Binary storage/upload remains an adapter concern outside the Catalog aggregate.
- Upload processing, AI-assisted alt text/caption review, and geolocation policy remain future design
  work.

See [Architecture flows](../architecture/FLOWS.md#media-gallery-and-image-metadata-flows)
for the media/gallery flow.

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

Current implementation applies `CatalogTourDraftCreated` only. The remaining events above describe the
accepted direction and should be added with their owning feature slices.

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

Current Admin event contract with Catalog consumer coverage:

- `AdminTourCreatedIntegrationEvent`.

Planned/evolving Admin event contracts:

- `AdminTourDetailsChangedIntegrationEvent`.
- `AdminTourScheduleChangedIntegrationEvent`.
- `AdminTourArchivedIntegrationEvent`.

Catalog consumers must be idempotent. The current consumer wrapper uses `IIdempotencyStore`; durable
Catalog inbox persistence is still part of the planned/evolving runtime path.

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

Current management endpoints:

- `GET /catalog/tours`.
- `GET /catalog/tours/{id}`.
- `PUT /catalog/tours/{id}/presentation`.

Planned/evolving management endpoints:

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
