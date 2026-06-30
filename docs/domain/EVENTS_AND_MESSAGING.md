# Events and Messaging

This document defines the durable event and messaging direction for ViajantesTurismo.

## Principles

- Domain events and integration events are separate concepts.
- Domain events belong to DDD aggregate logic and stay inside one bounded context.
- Integration events are explicit cross-boundary contracts.
- Event dispatch should extend `SharedKernel.Mediator` instead of creating a disconnected bus.
- Event dispatch APIs must remain typed and compiler-safe.
- CloudEvents are transport envelopes for integration events, not domain model primitives.
- Inbox, outbox, idempotency, and projections are infrastructure/runtime concerns, not aggregate
  responsibilities.

## SharedKernel Modules

### `SharedKernel.Domain`

Owns DDD primitives:

- `IIdentified<TId>`.
- `IEntity<TId>`.
- `IAggregateRoot`.
- `IAggregateRoot<TId>`.
- `Entity<TId>` while base-class consumers are migrated.
- `AggregateRoot<TId>`.
- `IDomainEvent`.
- Domain event recording and dequeueing.

### `SharedKernel.BuildingBlocks`

Owns reusable value objects and small cross-context primitives:

- `ValueObject`.
- `DateRange`.
- Future source-generated value-object conventions.

### `SharedKernel.DomainEvents`

Owns typed domain event dispatch:

- `IDomainEventDispatcher`.
- `IDomainEventHandler<TDomainEvent>`.
- Mediator adapter for in-process generated dispatch.

Domain event dispatch stays local to the bounded context. It does not use CloudEvents, inbox, or
outbox persistence by default.

### `SharedKernel.IntegrationEvents`

Owns typed integration event dispatch:

- `IIntegrationEvent`.
- `IIntegrationEventDispatcher`.
- `IIntegrationEventHandler<TIntegrationEvent>`.
- Event type and version conventions.

Integration events can be persisted and transported through adapters, but the core abstraction
project remains dependency-free.

### `SharedKernel.IntegrationEvents.CloudEvents`

Owns CloudEvents mapping:

- Typed integration event to CloudEvents mapping.
- CloudEvents to typed integration event mapping.
- CloudEvents source, subject, type, and content-type conventions.

Bounded-context domain and application projects should not depend directly on this adapter.

### `SharedKernel.Idempotency`

Owns idempotency abstractions:

- `IdempotencyKey`.
- `IdempotencyScope`.
- `IIdempotencyStore`.
- Processed operation or message identity.

Idempotency applies to integration inbox processing, command/request handling, projection
checkpointing, and future endpoint runtime flows. Persistence belongs in infrastructure or adapter
projects.

### `SharedKernel.EventSourcing`

Owns event-sourcing abstractions:

- `EventSourcedAggregateRoot<TId>`.
- `IEventStore`.
- `IEventSerializer`.
- `StreamId`.
- `StreamVersion`.
- `EventSequence`.
- `IProjection`.
- `IProjectionCheckpointStore`.

Event-sourcing infrastructure may use PostgreSQL first, but the SharedKernel abstractions should
remain storage-neutral.

### Provider Modules

Provider-specific reusable infrastructure belongs in `SharedKernel.<Capability>.<Provider>` modules.
For example, `SharedKernel.EventSourcing.PostgreSQL` contains PostgreSQL event-store and projection
checkpoint persistence, while `SharedKernel.EventSourcing` remains storage-neutral.

Bounded-context infrastructure owns composition, schema naming, migrations, read models, and
context-specific operational policy. See
[ADR-027](../adr/20260624-provider-specific-sharedkernel-infrastructure-modules.md) for naming and
boundary rules.

## Domain Events

Domain events describe business facts inside one bounded context. Aggregates raise them as part of
state transitions.

Domain events should:

- Be raised by aggregate behavior.
- Be meaningful within one bounded context.
- Avoid transport, serialization, and CloudEvents concerns.
- Be dispatched after successful domain work according to the owning application policy.
- Not automatically publish integration events.

Example shape:

```csharp
public interface IDomainEvent;

public interface IDomainEventDispatcher
{
    ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
        where TDomainEvent : IDomainEvent;
}

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask Handle(TDomainEvent domainEvent, CancellationToken ct);
}
```

## Integration Events

Integration events are explicit contracts between bounded contexts or external systems.

Integration events should:

- Be published intentionally by application or infrastructure boundary logic.
- Be versioned and named with stable event type identifiers.
- Avoid referencing domain entity types.
- Be persisted through outbox when they must be reliably published with local state changes.
- Be processed through inbox when consumed from another boundary.

Example shape:

```csharp
public interface IIntegrationEvent;

public interface IIntegrationEventDispatcher
{
    ValueTask Dispatch<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent;
}

public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    ValueTask Handle(TIntegrationEvent integrationEvent, CancellationToken ct);
}
```

## CloudEvents

CloudEvents are used at integration boundaries.

Recommended fields:

- `id`: integration event id, preferably Guid v7.
- `source`: bounded context or service, such as `ViajantesTurismo.Admin`.
- `type`: stable event type, such as `viajantesturismo.admin.tour.created.v1`.
- `subject`: aggregate or resource id.
- `time`: occurrence timestamp.
- `datacontenttype`: `application/json`.
- `dataschema`: optional schema URI when schema publication exists.

The typed event remains the code contract. CloudEvents is an adapter/envelope standard for
transport and interoperability.

## Inbox and Outbox

Inbox and outbox tables are part of the architecture.

### Outbox

Use an outbox when a bounded context publishes integration events as part of a state change.

Purpose:

- Persist integration events in the same transaction as local state changes.
- Dispatch later through a background dispatcher.
- Avoid event loss after database commit and before transport publish.

Ownership:

- Core contracts live in SharedKernel abstractions.
- Physical tables live in bounded-context infrastructure.
- Admin owns its integration outbox.
- Catalog owns its integration outbox only if it publishes integration events.

Recommended columns:

- `id`.
- `event_type`.
- `event_version`.
- `source`.
- `subject`.
- `occurred_at_utc`.
- `payload_json`.
- `metadata_json`.
- `correlation_id`.
- `causation_id`.
- `attempt_count`.
- `next_attempt_at_utc`.
- `processed_at_utc`.
- `failed_at_utc`.
- `last_error`.

### Inbox

Use an inbox when a bounded context consumes integration events from outside its transaction
boundary.

Purpose:

- Deduplicate at-least-once message delivery.
- Store receive and processing status.
- Support retries and diagnostics.

Ownership:

- Core idempotency contracts live in `SharedKernel.Idempotency`.
- Physical inbox tables live in consuming bounded-context infrastructure.
- Catalog owns its inbox for Admin-to-Catalog events.

Recommended columns:

- `message_id`.
- `event_type`.
- `event_version`.
- `source`.
- `subject`.
- `received_at_utc`.
- `processed_at_utc`.
- `status`.
- `attempt_count`.
- `last_error`.
- `payload_hash`.
- `correlation_id`.
- `causation_id`.

## Event Sourcing

Event-sourced aggregates persist state transitions as append-only event streams.

Event-sourced flows should include:

- Stream identity.
- Expected stream version for optimistic concurrency.
- Ordered event sequence numbers.
- Event metadata.
- Projection checkpoints.
- Replay and rebuild paths for read models.

Catalog tours use event sourcing because customer-facing content needs clear versioning, auditability,
and rebuildable projections.

## Future Runtime Direction

The repository may later add a small endpoint/runtime framework that integrates:

- Minimal API endpoint registration.
- Mediator command/query handlers.
- Domain event handlers.
- Integration event subscriptions.
- Event-sourced projections.
- Health checks and diagnostics.
- OpenAPI metadata.

Transport adapters should remain replaceable:

- In-process mediator.
- PostgreSQL inbox/outbox.
- CloudEvents HTTP.
- Dapr pub/sub.
- MassTransit.
- Future gRPC.

## Related Documentation

- [ADR-022: Split SharedKernel Domain and Building Blocks](../adr/20260621-split-sharedkernel-domain-and-building-blocks.md)
- [ADR-023: Separate Domain and Integration Event Dispatch](../adr/20260621-separate-domain-and-integration-event-dispatch.md)
- [ADR-024: CloudEvents as Integration Event Adapter](../adr/20260621-cloudevents-as-integration-event-adapter.md)
- [ADR-025: Event Source Catalog Tour Presentation](../adr/20260621-event-source-catalog-tour-presentation.md)
- [Catalog Bounded Context](../bounded-contexts/Catalog.md)
