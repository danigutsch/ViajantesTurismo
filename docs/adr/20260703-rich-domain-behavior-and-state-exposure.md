# ADR-031: Rich Domain Behavior and State Exposure

**Status**: Accepted — 2026-07-03

## Context

Catalog and Admin handlers have begun checking raw domain state to answer business questions, such
as media readiness or version compatibility. Those checks can spread domain decisions across
application handlers and make aggregates look like data bags instead of consistency boundaries.

Existing decisions already require validated factories, `Result`-returning updates, value objects
for related concepts, and persistence-agnostic domain types. This decision adds tactical rules for
what state may be exposed and when behavior must own the decision.

Research sources:

- Microsoft DDD guidance: aggregates enforce invariants, child changes go through aggregate
  behavior, collections should be read-only externally, and domain models should avoid hard EF Core
  dependencies.
- Microsoft domain validation guidance: entities should always be valid; invariants belong in
  constructors/factories and update methods, while cross-aggregate or persistence-backed rules stay
  in application services.
- EF Core guidance: constructor binding, private setters, backing fields, and field-only properties
  allow persistence without exposing setters or persistence-shaped APIs.
- Martin Fowler value object guidance: value objects are identified by values, should be immutable,
  and are useful for replacing primitive or repeated state/validation concepts.

## Decision

Model domain decisions as behavior first. Expose state only when callers need facts for display,
mapping, persistence, serialization, or simple filtering that does not decide a business outcome.

Use these rules for Catalog and Admin refactors:

- Keep aggregate and entity setters private. Mutations go through intent-revealing methods that
  return `Result` when they can fail.
- Expose collections as `IReadOnlyCollection<T>` or equivalent read-only views. Add, remove, and
  reorder operations stay on the aggregate or owning entity.
- Keep calculated facts as properties only when the calculation is pure, stable, and names a domain
  fact, for example `TotalPrice` or `PaymentStatus`.
- Prefer methods for context-bearing questions and decisions, for example readiness, lifecycle
  transitions, version compatibility, publishability, and media replacement safety.
- Do not let application handlers combine multiple raw properties to make the same business
  decision. Move repeated or meaning-rich checks behind domain behavior.
- Use enums for finite labels that have little or no behavior and are stable in the ubiquitous
  language.
- Wrap enum-plus-data or enum-plus-rules in a value object when validation, calculations, or related
  fields must stay together.
- Use a state object or explicit transition methods when each state has different allowed commands,
  transition rules, side effects, or error messages. Do not introduce polymorphic state only for one
  simple switch.
- Keep EF Core mapping in Infrastructure. Domain types must not depend on EF Core, data annotations
  for persistence, or persistence helper packages.
- Use EF Core constructor binding, private setters, backing fields, field-only properties, and owned
  mappings to preserve domain encapsulation. Keep private parameterless constructors only where EF
  still requires them for aggregate materialization.

## Consequences

### Pros

- Keeps business meaning close to aggregate invariants.
- Reduces duplicated property-combination checks in handlers.
- Preserves clean Architecture and EF Core boundaries.
- Gives the Catalog audit, Admin audit, Catalog media lifecycle, and Admin first-slice work a
  concrete rule set.

### Cons

- Some properties remain public for mapping and UI projection, so reviews must distinguish facts
  from decisions.
- Richer behavior can overgrow if every enum gains a strategy object. Refactors must stay small and
  justified by real rules or repeated callers.

## Apply now

- Catalog audit: list repeated raw state checks and choose method, calculated property, value
  object, or no change per hotspot.
- Admin audit: same audit, limited to Admin aggregate/application call sites.
- Catalog media lifecycle: move readiness/version/lifecycle checks behind media-domain behavior
  first.
- Admin first slice: implement one audited Admin hotspot after the Admin audit confirms scope.

## Defer

- Broad enum replacement without behavior differences.
- New abstraction layers, specifications, factories, or state-pattern hierarchies without at least
  two current callers or a real lifecycle rule.
- Persistence rewrites unrelated to encapsulation or EF boundary leaks.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-001: Domain Validation with Factory Methods][adr-001]
- Related: [ADR-010: Discount as Value Object with Audit Trail](20251108-discount-value-object.md)
- Related: [ADR-026: Domain Materialization and SharedKernel Persistence Boundaries][adr-026]
- Source: [Implementing a microservice domain model with .NET][ms-domain-model]
- Source: [Designing validations in the domain model layer][ms-domain-validation]
- Source: [Entity types with constructors][ef-constructors]
- Source: [Backing Fields](https://learn.microsoft.com/ef/core/modeling/backing-field)
- Source: [Value Object](https://martinfowler.com/bliki/ValueObject.html)

[adr-001]: 20251108-domain-validation-factory-methods.md
[adr-026]: 20260621-domain-materialization-and-sharedkernel-persistence-boundaries.md
[ms-domain-model]: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/net-core-microservice-domain-model
[ms-domain-validation]: https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations
[ef-constructors]: https://learn.microsoft.com/ef/core/modeling/constructors
