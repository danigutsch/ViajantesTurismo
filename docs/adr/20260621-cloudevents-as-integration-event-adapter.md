# ADR-024: CloudEvents as Integration Event Adapter

**Status**: Accepted - 2026-06-21

## Context

Integration events need stable metadata for event identity, source, type, subject, timestamp,
content type, and future interoperability. Dapr uses CloudEvents for pub/sub messages, and
CloudEvents is a standard envelope for event-driven integration.

At the same time, bounded-context domain and application projects should not depend on transport
envelope SDK types just to define typed integration event contracts.

## Decision

Use CloudEvents as an adapter for integration-event transport metadata, not as the core integration
event abstraction.

- `SharedKernel.IntegrationEvents` remains dependency-free and owns typed integration event
  contracts and dispatch abstractions.
- `SharedKernel.IntegrationEvents.CloudEvents` references the CloudEvents SDK and maps typed
  integration events to and from CloudEvents.
- Domain events never use CloudEvents.

## Consequences

- Integration event contracts stay typed and lightweight.
- CloudEvents interoperability is available at transport boundaries.
- Future Dapr-compatible pub/sub behavior can be added without changing domain/application code.
- Infrastructure or adapter layers must perform CloudEvents mapping explicitly.
- CloudEvents SDK dependencies are isolated to the adapter project.

## Alternatives Considered

1. **Reference CloudEvents SDK from the core integration event project**
   Rejected because it would leak transport envelope details into every bounded context that defines
   or handles integration events.

2. **Define a custom envelope instead of CloudEvents**
   Rejected because CloudEvents already covers the interoperability metadata needed for future
   transport adapters.

3. **Use CloudEvents for domain events too**
   Rejected because domain events are domain-model facts, not transport messages.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
