# ADR-025: Event Source Catalog Tour Presentation

**Status**: Accepted - 2026-06-21

## Context

Catalog tour content is customer-facing and needs clear version history, publish/unpublish history,
projection rebuilds, and future auditability. Public website read models should be optimized for
listing, detail pages, and search without becoming the source of truth.

Marten demonstrates a proven model for event streams, optimistic stream versioning, and rebuildable
projections. This solution should own its abstractions initially and use PostgreSQL as the first
storage target.

## Decision

Use event sourcing for Catalog tours and related customer-facing Catalog entities that require
versioned presentation history.

`CatalogTour` will be modeled as an event-sourced aggregate. Catalog stores append-only event streams,
enforces optimistic stream versioning, and builds read models through projections. Catalog may use the
same physical PostgreSQL resource as Admin initially, but it owns separate schema and tables.

Create `SharedKernel.EventSourcing` for storage-neutral event-sourcing abstractions. PostgreSQL
storage and projection persistence belong in bounded-context infrastructure or future adapter
projects.

## Consequences

- Catalog presentation changes are auditable and replayable.
- Public and management read models can be rebuilt from event streams.
- Optimistic stream versioning provides concurrency control for content editing.
- Projection code and checkpointing become required infrastructure.
- The first Catalog implementation is more complex than state-based CRUD.

## Alternatives Considered

1. **Use CRUD tables as the source of truth for Catalog tours**
   Rejected because it loses the explicit version history and projection rebuild path desired for
   customer-facing content.

2. **Use Admin tour state as the Catalog source of truth**
   Rejected because Admin operational state and Catalog presentation state have different lifecycles.

3. **Adopt Marten directly now**
   Deferred. Marten is a strong inspiration, but the repository should first define its own narrow
   event-sourcing abstractions and decide storage details through implementation issues.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Catalog Bounded Context](../bounded-contexts/Catalog.md)
- Related: [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
