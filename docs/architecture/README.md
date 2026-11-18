# Architecture Overview

This page links the current implementation shape to the longer-lived ADR and domain docs. Diagrams
show current repository structure unless a section is explicitly marked as planned. Generated
diagram sections are refreshed with
`dotnet tool run sharedkernel-docs -- generate --config docs/architecture/generated-diagrams.json`.

## System map

Start with the [system architecture diagram](system-overview.md) for a single map of users, web apps,
APIs, workers, data stores, messaging/outbox tables, SharedKernel modules, external dependencies, and
trust boundaries. Use [Diagram guidance](diagram-guidance.md) to choose diagram types and understand
which views are generated from code, config, or curated architecture metadata.

## Runtime resources

The Aspire AppHost is the source of truth for local runtime wiring. For the generated resource graph,
deployment mapping, service discovery, migration startup, and secret boundaries, see
[Runtime wiring and deployment mapping](runtime-wiring-and-deployment.md).

For database access surfaces, EF Core ownership, data handling classes, and persistence roles, see
[Data ownership and trust boundaries](data-ownership.md).

## Project boundary map

Keep business rules in domain projects. Keep provider-specific persistence and external adapters in
bounded-context infrastructure unless ADR-027's split threshold justifies a reusable
`SharedKernel.<Capability>.<Provider>` adapter package.

For the generated project reference map, bounded-context ownership, SharedKernel modules, and allowed
or forbidden dependency directions, see [Architecture boundaries and dependency flow](boundaries-and-dependencies.md).

## Admin-to-Catalog content workflow

See [Architecture flows](FLOWS.md) for Admin workflows, the implemented Admin-to-Catalog transport,
Catalog event-sourcing/projection flows, localized public-content review flows, media/gallery metadata
flows, and Branding module flows. Planned behavior remains explicitly marked.

### Implemented Admin-to-Catalog publication

```mermaid
sequenceDiagram
    participant Admin as Admin context
    participant Outbox as Admin outbox
    participant Relay as Admin relay
    participant Queue as Admin PostgreSQL transport
    participant Worker as Catalog.IntegrationEventWorker
    participant Inbox as Catalog idempotency
    participant Store as PostgreSQL event store
    participant Projection as Catalog projection
    participant Public as Public.Web

    Admin->>Outbox: Commit tour + envelope atomically
    Relay->>Outbox: Claim with lease
    Relay->>Queue: Publish transport message
    Worker->>Queue: Claim batch with SKIP LOCKED
    Worker->>Inbox: TryStart(source + event id)
    Worker->>Store: Invoke typed handler; append Catalog event
    Projection->>Store: Poll appended events after checkpoint
    Projection->>Projection: Update read model and checkpoint
    Public->>Projection: Read published tour presentation
```

Admin and Catalog use separate databases, which may share one PostgreSQL server. Admin owns its
`messaging` schema, outbox, and transport rows. Catalog owns its `messaging` schema and idempotency rows.
The worker reads Admin transport rows and performs Catalog handling through one scoped, sequential
claimed batch. See [Events and messaging](../domain/EVENTS_AND_MESSAGING.md) and Catalog ADRs in
[Architecture decisions](../ARCHITECTURE_DECISIONS.md#architecture--layers).

## Domain references

- [Admin bounded context](../bounded-contexts/Admin.md)
- [Branding](../branding.md)
- [Catalog bounded context](../bounded-contexts/Catalog.md)
- [Architecture flows](FLOWS.md)
- [Domain aggregates](../domain/AGGREGATES.md)
- [Glossary](../domain/GLOSSARY.md)

## Country and currency reference data

| Surface | Current responsibility |
| --- | --- |
| Management Web | `CountryService` loads the country selector catalog and translates known nationality demonyms for display. `CountrySelector` binds an alpha-2 code for addresses and a name for nationality. `CountryFormatter` resolves stored codes for detail and review pages. `CurrencyChoices` supplies display labels and form options. Forms own their initial selection. |
| Admin contracts | Customer address country and nationality travel as separate string fields. Tour, booking, and payment contracts carry ISO currency codes as strings with amounts in separate fields. These representations let clients exchange codes without depending on the Admin domain types. |
| Admin application and domain | Tour and payment handlers parse external codes through SharedKernel's `Currency`. The address value object validates assigned codes using SharedKernel's `CountryCode`. `FinancialCurrencies` allows BRL, EUR, and USD for tour pricing and allows EUR and BRL for obligations and settlements. Payment invariants further constrain specific operations. |
| Persistence and rendering | EF stores address country codes and rehydrates them through `CountryCode.Restore`. Payment serialization preserves currency codes. The payment instruction renderer uses invariant numeric formatting. Management Web owns its localized display formatting. |

Use SharedKernel BuildingBlocks as the canonical validator for assigned ISO codes. Keep
presentation labels, localized money formatting, and nationality compatibility in their consumers.
Keep eligibility, settlement rules, and any business defaults in Admin. A form's initial selection
is a UI convenience and Admin still validates submissions.

The Management Web country catalog is static deployment data. Its singleton service keeps the
loaded data for the host lifetime and retries a missing or malformed catalog on subsequent calls.
ISO code lookups use the SharedKernel value objects directly. Keep presentation catalogs
read-only and in process, with no request-specific or user-specific state in the cache.

The country selector and formatter use the deployed JSON catalog and SharedKernel English
names, respectively. Before unifying these sources, compare supported address
codes, nationality aliases, labels, and flag coverage. The tour and settlement forms also
duplicate Admin's allowed subsets. If those subsets change, publish the selectable options
through Admin's contracts or an application-owned read endpoint and verify the form against
the corresponding command validation. Booking details format the price and absolute discount in
the booking currency. The edit form labels the absolute discount with the same currency. Amounts
paid and remaining balances are in EUR because they describe approved payment obligations. The
booking views cover BRL and USD alongside EUR.

## Measurement and media ownership

- [Coupling and cohesion proposal](coupling-and-cohesion-metrics.md) defines advisory measurement and rollout criteria.
- [Media ownership decision](../adr/20261020-catalog-media-metadata-and-object-ownership.md) separates Catalog metadata from object bytes and repair authority.

## Migration planning

The [application vertical slice plan](../vertical-slice-migration-plan.md) inventories current
feature ownership and proposes incremental moves within existing Clean Architecture layers.

## Flow references

- [Architecture boundaries and dependency flow](boundaries-and-dependencies.md)
- [System architecture diagram](system-overview.md)
- [Diagram guidance](diagram-guidance.md)
- [Multi-store consistency audit](multi-store-consistency-audit.md)
- [Runtime wiring and deployment mapping](runtime-wiring-and-deployment.md)
- [CI and local validation flow](ci-validation-flows.md)
- [Observability signal and dashboard consumption flows](observability-consumption-flows.md)

## Current public content status

- Core public website content variants and localization are implemented through Catalog-owned content
  contracts and Public.Web rendering, separate from core Admin CRUD.
- Configurable Branding is an independent ViajantesTurismo module; `SharedKernel.Branding` retains
  only pure validation/normalization. The standalone API is current, while
  [ADR-040](../adr/20260724-branding-module-co-hosting-and-state-distribution.md) accepts a gated move
  into Catalog.Api without transferring contracts, routes, permissions, schema, or outbox
  ownership to Catalog. The separation remains logical; the co-hosted modules share one process and
  credential blast radius.
- Catalog media upload, management metadata, accessibility drafts, processing, reconciliation, and
  public ready-image filtering are implemented; event-sourced gallery editing, evaluation dashboards,
  and optional storage adapters remain planned.
- Adapter package splits should follow ADR-027's capability-first naming, dependency-direction, and
  split-threshold rules.
