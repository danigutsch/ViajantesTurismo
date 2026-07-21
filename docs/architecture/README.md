# Architecture Overview

This page links the current implementation shape to the longer-lived ADR and domain docs. Diagrams
show current repository structure unless a section is explicitly marked as planned. Generated
diagram sections are refreshed with `bash scripts/update-architecture-diagrams.sh`.

## System map

Start with the [system architecture diagram](system-overview.md) for a single map of users, web apps,
APIs, workers, data stores, messaging/outbox tables, SharedKernel modules, external dependencies, and
trust boundaries. Use [Diagram guidance](diagram-guidance.md) to choose diagram types and understand
which views are generated from code, config, or curated architecture metadata.

## Runtime resources

The Aspire AppHost is the source of truth for local runtime wiring. For the generated resource graph,
deployment mapping, service discovery, migration startup, and secret boundaries, see
[Runtime wiring and deployment mapping](runtime-wiring-and-deployment.md).

## Project boundary map

Keep business rules in domain projects. Keep provider-specific persistence and external adapters in
bounded-context infrastructure unless ADR-027's split threshold justifies a reusable
`SharedKernel.<Capability>.<Provider>` adapter package.

For the generated project reference map, bounded-context ownership, SharedKernel modules, and allowed
or forbidden dependency directions, see [Architecture boundaries and dependency flow](boundaries-and-dependencies.md).

## Admin-to-Catalog content workflow

See [Architecture flows](FLOWS.md) for Admin workflows, the implemented Admin-to-Catalog transport,
Catalog event-sourcing/projection flows, localized public-content review flows, media/gallery metadata
flows, and Branding adapter flows. Planned behavior remains explicitly marked.

### Implemented Admin-to-Catalog publication

```mermaid
sequenceDiagram
    participant Admin as Admin context
    participant Outbox as Admin outbox
    participant Relay as Admin relay
    participant Queue as Admin PostgreSQL transport
    participant Worker as IntegrationEventWorker
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
- Configurable branding now belongs to `SharedKernel.Branding` plus the ViajantesTurismo Branding API
  adapter. Catalog does not own Branding routes or clients.
- Catalog media upload, management metadata, accessibility drafts, processing, reconciliation, and
  public ready-image filtering are implemented; event-sourced gallery editing remains planned.
- Adapter package splits should follow ADR-027's capability-first naming, dependency-direction, and
  split-threshold rules.
