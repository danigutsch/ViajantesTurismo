# ADR-023: Separate Domain and Integration Event Dispatch

**Status**: Accepted - 2026-06-21

## Context

Domain events and integration events have different roles. Domain events describe facts inside one
bounded context and belong to aggregate/application logic. Integration events are explicit contracts
for crossing bounded-context or process boundaries.

The repository already has `SharedKernel.Mediator` with generated dispatch, notification handlers,
module discovery, ordering, and dispatch strategy support. New event dispatch should extend this
model instead of introducing a disconnected event bus.

## Decision

Create separate typed dispatch modules for domain events and integration events.

- `SharedKernel.DomainEvents` owns `IDomainEventDispatcher` and
  `IDomainEventHandler<TDomainEvent>`.
- `SharedKernel.IntegrationEvents` owns `IIntegrationEventDispatcher` and
  `IIntegrationEventHandler<TIntegrationEvent>`.

Both modules may adapt to `SharedKernel.Mediator` internally, but their public abstractions stay
separate and compiler-safe. Domain events do not automatically publish integration events.

## Consequences

- Code cannot accidentally handle an integration event through the domain event API or vice versa.
- Domain event handlers remain local to bounded contexts.
- Integration event publishing remains an explicit application/infrastructure decision.
- Existing mediator source generation can remain the shared dispatch mechanism.
- Additional adapters are needed for durable outbox/inbox and external transports.

## Alternatives Considered

1. **Use `INotification` directly for all events**
   Rejected because it hides the lifecycle difference between domain and integration events.

2. **Create a separate event bus unrelated to `SharedKernel.Mediator`**
   Rejected because it duplicates existing generated dispatch capabilities.

3. **Automatically convert domain events to integration events**
   Rejected because it couples internal aggregate facts to cross-context contracts.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
