# Architecture Flows

These diagrams separate implemented behavior from planned/evolving behavior. They are source-controlled
Mermaid diagrams so they can be reviewed with the surrounding Markdown.

## Admin workflows

### Current implementation

Admin owns operational tour, customer, booking, and payment workflows. The API routes map to
application handlers, handlers enforce cross-aggregate checks through stores, domain aggregates enforce
invariants, and EF Core persists state through the Admin write database.

```mermaid
flowchart LR
    management[Management.Web]
    api[Admin.ApiService endpoints]
    handler[Admin.Application handler]
    store[Admin store/query service]
    domain[Admin.Domain aggregate]
    db[(PostgreSQL Admin tables)]

    management --> api
    api --> handler
    handler --> store
    handler --> domain
    domain --> handler
    handler --> db
    api --> store
    store --> db
```

Implemented workflow groups:

- Tours: create, update, list, get by id.
- Customers: create, update, list, get by id, import preview/commit flow.
- Bookings: create, confirm, cancel, complete, delete pending bookings, update notes, update discount,
  update details, record payments, and query by tour/customer.
- Payments: recorded through the booking aggregate; payment status is calculated from payments and
  booking total.

Tour creation currently dispatches `AdminTourCreatedIntegrationEvent` after `SaveEntities(ct)` through
the in-process `ServiceProviderIntegrationEventDispatcher`.

```mermaid
sequenceDiagram
    participant API as Admin.ApiService
    participant Handler as CreateTourCommandHandler
    participant Store as ITourStore
    participant Tour as Tour aggregate
    participant Db as Admin PostgreSQL
    participant Dispatcher as In-process integration dispatcher

    API->>Handler: CreateTourCommand
    Handler->>Store: IdentifierExists(identifier)
    Handler->>Tour: Tour.Create(definition)
    Tour-->>Handler: Result<Tour>
    Handler->>Db: SaveEntities(ct)
    Handler->>Dispatcher: Dispatch(AdminTourCreatedIntegrationEvent)
    Dispatcher-->>Handler: Returns after registered local handlers run
    Handler-->>API: Result<Guid>
```

### Planned/evolving

Durable Admin-to-Catalog publication is planned, not fully wired in the current runtime. The event and
messaging docs describe outbox/inbox direction; current Admin production code does not persist an
Admin outbox row or transport the event to Catalog.ApiService.

```mermaid
flowchart LR
    adminHandler[Admin handler]
    adminDb[(Admin tables)]
    outbox[(Admin outbox planned)]
    transport[Transport adapter planned]
    inbox[(Catalog inbox planned)]
    catalogConsumer[Catalog consumer]

    adminHandler --> adminDb
    adminHandler -. planned .-> outbox
    outbox -. planned .-> transport
    transport -. planned .-> inbox
    inbox -. planned .-> catalogConsumer
```

## Catalog event sourcing and projection flows

### Current implementation

Catalog has event-sourcing abstractions and tested application components for consuming
`AdminTourCreatedIntegrationEvent`, creating a `CatalogTourDraftCreated` event, and projecting it into
`CatalogTourReadModels`. The Catalog API currently exposes read-model CRUD-style endpoints for tour
presentation and public published tour reads.

```mermaid
sequenceDiagram
    participant Event as AdminTourCreatedIntegrationEvent
    participant Idem as IdempotentIntegrationEventConsumer
    participant Consumer as AdminTourCreatedIntegrationEventConsumer
    participant Aggregate as CatalogTour.CreateDraft
    participant Store as IEventStore
    participant Runner as CatalogProjectionRunner
    participant Projection as CatalogTourReadModelProjection
    participant ReadModel as ICatalogTourReadModelStore

    Event->>Idem: Handle(event)
    Idem->>Consumer: Handle(event) when idempotency starts
    Consumer->>Aggregate: Create draft
    Aggregate-->>Consumer: CatalogTourDraftCreated
    Consumer->>Store: Append(stream, NoStream, events)
    Runner->>Store: LoadAfter(checkpoint, 100)
    Runner->>Projection: Apply(envelope)
    Projection->>ReadModel: UpsertDraft(...)
    Runner->>Runner: Save projection checkpoint
```

Current runtime limits:

- `CatalogTour` currently applies `CatalogTourDraftCreated` only.
- `PUT /catalog/tours/{id}/presentation` updates the read model directly; it does not append a
  Catalog tour presentation event yet.
- Public endpoints read only rows marked `IsPublished` from the read model.
- Projection runner and integration consumer are application components with unit coverage; production
  DI wiring for event store, idempotency store, and background projection execution is still evolving.
- `CatalogTelemetry` emits OpenTelemetry activities and counters around integration event handling,
  idempotency decisions, tour stream updates, and projection batches.

### Planned/evolving

ADR-025 remains the direction for versioned Catalog tour presentation. Future slices should move
presentation edits and publication transitions behind event-sourced aggregate commands before treating
read models as rebuildable source-of-truth projections.

```mermaid
flowchart TB
    edit[Management presentation edit]
    command[Catalog command handler planned]
    aggregate[CatalogTour aggregate]
    events[(catalog.events)]
    projector[Projection runner]
    managementRead[(Management read model)]
    publicRead[(Published public read model)]
    publicWeb[Public.Web]

    edit -. planned .-> command
    command -. planned .-> aggregate
    aggregate -. planned .-> events
    events -. planned .-> projector
    projector -. planned .-> managementRead
    projector -. planned .-> publicRead
    publicWeb -. planned .-> publicRead
```

## Public content localization and review flows

### Current implementation

Catalog owns editable public content for `en-US` and `pt-BR` variants. The current API lets management
clients list, get, and upsert content entries. The domain marks entries as `ReviewRequired` when any
variant has `RequiresHumanReview`; `Publish()` blocks publication while review is required.

```mermaid
sequenceDiagram
    participant Editor as Management.Web editor
    participant API as Catalog.ApiService
    participant Content as EditablePublicContent
    participant Store as IPublicContentStore
    participant Db as PublicContent tables

    Editor->>API: PUT /catalog/public-content/{**key}
    API->>Content: Create(key, sourceLanguage, variants)
    Content-->>API: Draft or ReviewRequired state
    API->>Store: SaveContent(content)
    Store->>Db: Upsert PublicContent + variants
    API-->>Editor: PublicContentDto
```

Current behavior:

- Supported variants are explicit: `en-US` and `pt-BR`.
- Both variants are required for each editable content entry.
- Machine-translated or AI-assisted content is represented by `RequiresHumanReview` on the variant.
- Public content tables persist entries and variants.
- Management-facing routes use `/catalog/public-content/{**key}` so stable content keys can contain
  path separators.
- Public reads use `GET /public/catalog/content/{**key}` and return published content only, selecting
  the requested approved language variant with fallback behavior.
- Upsert publishes immediately when no variant requires review; otherwise content remains
  review-required.

### Planned/evolving

Explicit review approval and manual publish endpoints are planned/evolving. Do not assume automatic
translation or auto-approval until those slices exist.

```mermaid
flowchart LR
    draft[Draft localized variants]
    review[Human review]
    publish[Publish when no review required]
    published[(Published content)]
    publicWeb[Public.Web]

    draft -->|RequiresHumanReview=true| review
    draft -->|RequiresHumanReview=false| publish
    review -. planned approval endpoint .-> publish
    publish --> published
    publicWeb -->|published-only read| published
```

## Media, gallery, and image metadata flows

### Current implementation

Catalog owns customer-facing image metadata and tour associations. Binary storage remains outside the
Catalog aggregate; Catalog stores safe public URIs, alt text, captions, attribution, tags, ordering,
cover-image flags, processing status, and responsive variants.

```mermaid
flowchart LR
    management[Management.Web media editor]
    api[Catalog.ApiService]
    store[IPublicMediaImageStore]
    db[(PublicMediaImages tables)]
    mapper[MapTour]
    dto[CatalogTourDto.Images]
    publicWeb[Public.Web gallery]

    management --> api
    api -->|PUT /catalog/media/images/{id}| store
    store --> db
    api -->|GET /catalog/tours/{id}/images| store
    api --> mapper
    mapper --> dto
    publicWeb --> dto
```

Current constraints visible in contracts:

- `Uri` is required.
- `AltText` is required and length-limited for accessibility.
- `Caption` is optional and length-limited.
- Public tour endpoints filter images to `Ready` processing status.

### Planned/evolving

Future media work should move gallery metadata changes behind Catalog event-sourced commands if tour
presentation read models become rebuildable from event streams.

```mermaid
flowchart TB
    upload[Media upload/storage adapter planned]
    asset[(Stored media asset planned)]
    metadata[Catalog image metadata]
    review[Alt text/caption review planned]
    projection[Published tour projection planned]
    publicWeb[Public.Web gallery]

    upload -. planned .-> asset
    asset -. public URI .-> metadata
    metadata -. requires alt text .-> review
    review -. approved .-> projection
    metadata -. event-sourced changes planned .-> projection
    publicWeb --> projection
```

Open design points for future issues:

- Storage provider and upload policy.
- Image ordering, hero-image selection, and removal behavior.
- Whether image metadata changes are Catalog tour events or a separate media stream.
- Accessibility review requirements beyond required `AltText`.

## References

- [Architecture overview](README.md)
- [Catalog bounded context](../bounded-contexts/Catalog.md)
- [Events and messaging](../domain/EVENTS_AND_MESSAGING.md)
- [Aggregates](../domain/AGGREGATES.md)
- [Domain validation](../DOMAIN_VALIDATION.md)
- [ADR-020: Web Frontends by Audience, Not by Bounded Context](../adr/20260523-web-frontends-by-audience-not-by-bounded-context.md)
- [ADR-021: Catalog Bounded Context for Public Tour Presentation](../adr/20260621-catalog-bounded-context-for-public-tour-presentation.md)
- [ADR-025: Event Source Catalog Tour Presentation](../adr/20260621-event-source-catalog-tour-presentation.md)
