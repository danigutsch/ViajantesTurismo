# ADR-039: Generated Messaging Dispatch Ownership

**Status**: Accepted - 2026-07-19

**Supersedes**: [ADR-023: Separate Domain and Integration Event Dispatch](20260621-separate-domain-and-integration-event-dispatch.md)

## Context

The mediator, domain-event, and integration-event stacks accumulated adapters, registration objects,
dictionaries, and service-provider lookup after closed generic types had already been known at compile
time. That duplicated dispatch ownership and made composition order determine which
`IDomainEventDispatcher` implementation won.

A bounded comparison of Mediator 3.0.2, Immediate.Handlers 3.11.1, Foundatio.Mediator 1.3.3, and
FastEndpoints showed that direct generated methods and exhaustive cases keep consumer glue smaller and
more explicit than runtime type recovery. The research supports generation of mechanical binding, not
adoption of another mediator framework or generation of business logic.

## Decision

- The mediator generator emits typed request, stream, notification, handler, and pipeline paths.
  Generic sender and publisher APIs remain for real interface-typed and object-dispatch callers, and
  forward through exhaustive generated cases.
- `AppMediator` receives closed typed `Func<T>` handler and pipeline dependencies. The factories defer
  scoped resolution until dispatch, avoiding constructor cycles without a general runtime registry.
- Domain-event provider generators emit closed `IDomainEventDispatchHandler` implementations. The
  scoped `CompositeDomainEventDispatcher` composes outbox and audit handlers without registration-order
  ownership of `IDomainEventDispatcher`.
- Each host supplies explicit `JsonTypeInfo<T>` metadata. Generated serializers and envelope publishers
  use closed typed cases instead of contract registration objects or dictionaries.
- Background consumers retain the genuine scope boundary. One scope owns each claimed batch and
  resolves one generated publisher with its closed typed handlers. The transport consumer passes that
  batch's messages to the publisher sequentially and forwards cancellation without generating business
  logic.
- Outbox and inbox registration remain separate. Producer contexts do not acquire inbox schema or
  idempotency services implicitly.

## Measured shape

Representative tests measure structure rather than enforce arbitrary line quotas:

| Path | Before | After |
| --- | --- | --- |
| Mediator ordinary dispatch | Typed switch plus dispatch-time `IServiceProvider` recovery in request, stream, pipeline, and notification paths | Same exhaustive generic forwarding with zero service-provider sites in ordinary dispatch |
| Domain event mapping | Competing mediator adapter and integration-event mapper registrations | One exhaustive generated mapper and one dispatcher registration |
| Integration envelopes | Four runtime registry/registration types, two dictionaries, and scoped service location | One generated serializer, one generated scoped publisher, typed `JsonTypeInfo<T>` and handler dependencies, no registry |

The external comparison above remains rationale rather than a line-count quota. Counts guide review;
preserving validation, transactions, cancellation, outbox retry, serialization compatibility, and
idempotency is the acceptance boundary.

## Consequences

- Missing and duplicate integration-event consumer handlers fail compilation with `SKMSG001` and
  `SKMSG002`; duplicate consumer event types fail compilation with `SKMSG003`; invalid registered
  contract types fail compilation with `SKMSG006`.
- Generated constructor size grows with the closed handler-factory set, while concrete handler and
  pipeline registrations remain scoped and visible to DI validation.
- Adding a contract requires explicit metadata and generated host composition.
- Dynamic runtime registries are not retained without a demonstrated dynamic caller.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
- [SharedKernel Packaging](../SHAREDKERNEL_PACKAGING.md)
- [Separate Domain and Integration Event Dispatch](20260621-separate-domain-and-integration-event-dispatch.md)
