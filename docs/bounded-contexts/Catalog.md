# Catalog Bounded Context

The Catalog bounded context owns customer-facing tour presentation for public discovery and public
website experiences.

Catalog does not own branding changes. Shared branding rules belong in `SharedKernel.Branding`, and
the ViajantesTurismo Branding adapter owns app-specific persistence, API routes, defaults, and public
rendering contracts.

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
- **Branding relationship** - Do not add branding behavior to Catalog. Use the Branding adapter.

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

- Admin maps a tour-created domain event into `AdminTourCreatedIntegrationEvent` during `SaveChanges`
  and commits it with the Admin outbox row.
- The Admin relay writes the Admin PostgreSQL transport table. `IntegrationEventWorker` claims batches,
  invokes the generated typed Catalog publisher, and applies Catalog inbox/idempotency before appending
  `CatalogTourDraftCreated`. The worker also hosts Catalog background projection execution.
- Admin and Catalog use separate databases, which may share one PostgreSQL server. Each database owns
  its own `messaging` schema and migrations.

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

- `CatalogTour` creates and applies `CatalogTourDraftCreated`, `CatalogTourPresentationChanged`,
  `CatalogTourPublished`, and `CatalogTourUnpublished`.
- Unpublished management presentation edits and explicit publish/unpublish transitions append to the
  tour stream; projections update `CatalogTourReadModels` for management and public reads.
- Mutations attempt inline projection. A committed mutation whose projection is deferred returns
  `202 Accepted`; the projection runner retries it from the unchanged checkpoint.
- Public tour DTO images are populated from Catalog media metadata and include only images whose
  processing and accessibility-review status is ready.

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
- Draft creation uses the normalized Admin identifier when available and an id-based fallback when
  that initial slug is already owned.
- Concurrent claims for the same normalized slug are serialized across application instances before
  Catalog checks availability against tour event streams and persists the presentation change. The
  unique `CatalogTourReadModels.Slug` database index independently rejects projection collisions.
  Optimistic stream versioning continues to protect edits to the individual tour stream.
- Published tours must be unpublished before presentation edits, including slug changes.

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

- `CatalogTourImageDto` defines public image metadata with an image identifier, reviewed `AltText` or an
  explicit decorative-image decision, optional `Caption`, responsive rendition metadata, ordering, and
  cover-image data.
- `CatalogTourDto.Images` is populated on management and public tour responses.
- `POST /catalog/tours/{id}/images` accepts validated multipart uploads and creates Catalog-owned image
  metadata and tour associations; `GET /catalog/tours/{id}/images` returns management metadata without
  storage keys or storage URIs.
- `POST /catalog/media/images/{id}/accessibility-draft` uses the SharedKernel LiteLLM-compatible image
  text generator to create review-required alt text/caption drafts from stored image bytes plus optional
  editorial context and trusted geolocation metadata.
- Public endpoints expose only images whose default accessibility text is reviewed and either non-empty
  or explicitly decorative.
- Media object reconciliation compares deterministic `media/` storage keys with live Catalog metadata,
  reports missing and orphaned objects, preserves recent orphans during a grace period, and deletes
  eligible orphaned objects from the integration-event worker.

Planned/evolving:

- Gallery changes should move behind event-sourced Catalog commands before image read models are
  treated as rebuildable projections.
- Binary storage/upload remains an adapter concern outside the Catalog aggregate.
- Upload processing remains asynchronous future work after original storage.
- Quality evaluation remains future work; the Catalog model already enforces review-required generated
  drafts before publication.
- Grafana-visible observability for generation, evaluation, review outcomes, and publication blockers
  remains future work.
- Geolocation policy for supplied generation context remains future design work.

See [Architecture flows](../architecture/FLOWS.md#media-gallery-and-image-metadata-flows)
for the media/gallery flow.

## Event Sourcing

Catalog tours use append-only event streams.

Implemented domain events:

- `CatalogTourDraftCreated`.
- `CatalogTourPresentationChanged`.
- `CatalogTourPublished`.
- `CatalogTourUnpublished`.

Planned/evolving domain events:

- `CatalogTourGalleryChanged`.
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

Current Admin event contract with Catalog consumer coverage:

- `AdminTourCreatedIntegrationEvent`.

Planned/evolving Admin event contracts:

- `AdminTourDetailsChangedIntegrationEvent`.
- `AdminTourScheduleChangedIntegrationEvent`.
- `AdminTourArchivedIntegrationEvent`.

Catalog consumers must be idempotent. The consumer wrapper uses `IIdempotencyStore`; the
`SharedKernel.Idempotency.EntityFrameworkCore` provider maps durable entries to Catalog's
`messaging.idempotency_keys`. PostgreSQL ingress claims with `FOR UPDATE SKIP LOCKED`, lease, and retry
state. One worker scope owns a claimed batch and its generated publisher. Messages are passed
sequentially; each envelope gets an asynchronously disposed child scope that resolves the closed typed
handler, preserving Catalog's idempotency decorator boundary.

## Persistence

Catalog and Admin use separate PostgreSQL databases. Those databases may share one PostgreSQL server,
but each bounded context owns its own schemas, tables, and migration history.

Expected tables:

- `catalog.event_streams`.
- `catalog.events`.
- `catalog.projection_checkpoints`.
- `messaging.idempotency_keys` for integration-event delivery guards.
- `messaging.outbox_messages` for Catalog media integration events and relay retry state.
- Catalog read-model tables for management and public projections.

## API Surface

Management-facing endpoints should edit customer-facing presentation data only.

Current management endpoints:

- `GET /catalog/tours`.
- `GET /catalog/tours/{id}`.
- `PUT /catalog/tours/{id}/presentation`.
- `POST /catalog/tours/{id}/publish`.
- `POST /catalog/tours/{id}/unpublish`.

Public-facing endpoints should return published projections only.

Initial public endpoints:

- `GET /public/catalog/tours`.
- `GET /public/catalog/tours/{slug}`.

The public list uses `TourSummaryDto`, while the detail endpoint uses `TourDetailsDto`. Both are
separate from management `CatalogTourDto` and expose published projections only. The detail contract
adds description, itinerary, SEO metadata, reviewed images, and `UpdatedAt`; the summary contract
contains title, slug, summary, reviewed images, and `UpdatedAt`.

## Related Documentation

- [ADR-021: Catalog Bounded Context for Public Tour Presentation](../adr/20260621-catalog-bounded-context-for-public-tour-presentation.md)
- [ADR-025: Event Source Catalog Tour Presentation](../adr/20260621-event-source-catalog-tour-presentation.md)
- [ADR-020: Web Frontends by Audience, Not by Bounded Context](../adr/20260523-web-frontends-by-audience-not-by-bounded-context.md)
- [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
- [Branding](../branding.md)
