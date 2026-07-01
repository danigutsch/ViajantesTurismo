# ADR-022: Split SharedKernel Domain and Building Blocks

**Status**: Accepted - 2026-06-21

## Context

`ViajantesTurismo.Common` originally contained reusable domain primitives such as `Entity<TId>`,
`ValueObject`, and `DateRange`. New Catalog, event sourcing, and event dispatch work needs these
primitives without keeping a broad catch-all common project.

The solution already uses focused SharedKernel projects for mediator, results, functional helpers,
OpenAPI, and observability. Reusable DDD primitives should follow the same focused package model.

## Decision

Create focused SharedKernel projects for DDD primitives and reusable value objects.

- `SharedKernel.Domain` owns identity interfaces, aggregate-root contracts, and `IDomainEvent`.
- `SharedKernel.BuildingBlocks` owns reusable value objects such as `ValueObject` and `DateRange`.

Migrate `ViajantesTurismo.Common` gradually into these projects. Decide separately whether `Currency`
and sanitizers belong in `SharedKernel.BuildingBlocks`, another focused SharedKernel project, or an
owning bounded context.

## Consequences

- DDD primitives become available to Admin, Catalog, and future bounded contexts without a catch-all
  common dependency.
- Aggregate root and domain event recording can be standardized across bounded contexts.
- Value objects that are truly reusable get a clear home.
- Existing code must be migrated carefully to avoid large noisy changes.
- `ViajantesTurismo.Common.BuildingBlocks.Entity<TId>` was removed after Admin moved to
  SharedKernel identity interfaces and generated identity support.
- SharedKernel base-class consumers moved toward `IIdentified<TId>`, `IEntity<TId>`,
  `IAggregateRoot<TId>`, and opt-in generated identity support before the base classes were removed.
- Future Vogen-like source generation can be added around `SharedKernel.BuildingBlocks` without
  blocking current Catalog work.

## Alternatives Considered

1. **Keep extending `ViajantesTurismo.Common`**
   Rejected because it would continue broad shared-project growth without clear ownership.

2. **Put all primitives in one `SharedKernel.Domain` project**
   Rejected because reusable value objects and DDD aggregate primitives have different consumers and
   future tooling needs.

3. **Duplicate primitives in each bounded context**
   Rejected because aggregate and value-object conventions should remain consistent across the
   solution.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Events and Messaging](../domain/EVENTS_AND_MESSAGING.md)
