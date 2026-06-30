# Configurable Model Source Generation

This design note defines the option shape for configurable model generation, records the first
repository audit, and gives a migration plan away from shared entity base classes.

## Goals

- Keep business models focused on invariants and behavior.
- Generate only explicitly requested support code.
- Allow model generation outside Domain projects when the target model opts in.
- Keep dependency-backed generation optional and dependency-aware.
- Avoid runtime reflection, runtime registries, broad wrappers, and generated business rules.

## Configuration model

Use an attribute-first model with optional assembly defaults. Attributes keep the opted-in type and
its generated behavior reviewable in the same diff.

```csharp
[GenerateModelSupport(Identity = true)]
public sealed partial class Customer : IIdentified<Guid>
{
    public Guid Id { get; private init; }
}
```

```csharp
[GenerateValueObject(UnderlyingType = typeof(string), Parsing = true)]
public readonly partial record struct TourCode;
```

```csharp
[assembly: GenerateModelSupportDefaults(Identity = true)]
```

Per-type attributes override assembly defaults. Defaults stay conservative: no type receives
generated code unless it opts in directly or an assembly explicitly opts in and the type has a marker
interface or attribute that makes the target unambiguous.

### Option groups

| Option | Default | Dependency | Notes |
| --- | --- | --- | --- |
| `Identity` | Off | None | Emits equality/hash helpers for `IIdentified<TId>` models. |
| `ValueObject` | Off | None | Emits scalar wrappers, `TryCreate`, parse/format helpers, and converters. |
| `Validation` | Off | None | Emits guard plumbing from explicit validation attributes only. |
| `Json` | Off | `System.Text.Json` | Emits `JsonConverter` and context registration helpers only when requested. |
| `EfCore` | Off | `Microsoft.EntityFrameworkCore` | Emits mapping helpers only when requested and EF Core is referenced. |
| `DtoConversion` | Off | None | Deferred; only when source and target are explicit and same-boundary safe. |

DDD/core generation is dependency-free and available when explicitly enabled for an opted-in model.
Cross-cutting support that depends on packages, such as EF Core or `System.Text.Json`, must either be
explicitly enabled or enabled only when the dependency exists and assembly defaults requested it.

### Diagnostics

The generator should fail fast with diagnostics for unsupported combinations:

- EF Core generation requested but EF Core is not referenced.
- JSON generation requested for a non-public contract shape that cannot be serialized safely.
- Identity generation requested on a type without a readable identifier.
- Value-object parsing requested without an underlying scalar type.
- DTO conversion requested across disallowed project boundaries.

Diagnostics should name the attribute option, the affected type, and the smallest corrective action.

### Generated output rules

- Deterministic file names: `<TypeName>.ModelSupport.g.cs`, `<TypeName>.EfCore.g.cs`,
  `<TypeName>.Json.g.cs`.
- Generated code under the target type namespace.
- Public generated APIs documented with XML comments.
- No generated persistence behavior should replace explicit domain invariants.
- Tests should cover option precedence, diagnostics, and stable generated text.

## Repository audit

### High payoff, low risk

1. Identity/equality boilerplate currently centralized by base classes:
   - `src/SharedKernel/SharedKernel.Domain/Entity.cs`
   - `src/SharedKernel/SharedKernel.Domain/AggregateRoot.cs`
   - `src/ViajantesTurismo.Common/BuildingBlocks/Entity.cs`
   - `src/ViajantesTurismo.Admin.Domain/Customers/Customer.cs`
   - `src/ViajantesTurismo.Admin.Domain/Tours/Tour.cs`
   - `src/ViajantesTurismo.Admin.Domain/Tours/Booking.cs`
   - `src/ViajantesTurismo.Admin.Domain/Tours/Payment.cs`
   - `src/ViajantesTurismo.Catalog.Domain/PublicContent/EditablePublicContent.cs`

2. Scalar and small value objects that repeat equality or factory shape:
   - `src/SharedKernel/SharedKernel.EventSourcing/StreamId.cs`
   - `src/SharedKernel/SharedKernel.EventSourcing/StreamRevision.cs`
   - `src/SharedKernel/SharedKernel.EventSourcing/ExpectedStreamRevision.cs`
   - `src/SharedKernel/SharedKernel.Idempotency/IdempotencyScope.cs`
   - `src/SharedKernel/SharedKernel.Idempotency/IdempotencyOperation.cs`
   - `src/ViajantesTurismo.Admin.Domain/Tours/BookingRoom.cs`
   - `src/ViajantesTurismo.Catalog.Domain/Media/MediaImageDimensions.cs`

3. JSON serializer context registration that is mechanical and easy to miss:
   - `src/ViajantesTurismo.Admin.ApiService/JsonSerializerContext.cs`

### Medium payoff, higher risk

1. EF Core mapping for repeated key and owned-value-object configuration:
   - `src/ViajantesTurismo.Admin.Infrastructure/ModelConfigurations/CustomerConfiguration.cs`
   - `src/ViajantesTurismo.Admin.Infrastructure/ModelConfigurations/TourConfiguration.cs`
   - `src/ViajantesTurismo.Admin.Infrastructure/Bookings/BookingConfiguration.cs`
   - `src/ViajantesTurismo.Admin.Infrastructure/Bookings/PaymentConfiguration.cs`
   - `src/ViajantesTurismo.Catalog.Infrastructure/ModelConfigurations/EditablePublicContentConfiguration.cs`
   - `src/ViajantesTurismo.Catalog.Infrastructure/ModelConfigurations/PublicMediaImageConfiguration.cs`

2. Contract DTO metadata and validation shape:
   - `src/ViajantesTurismo.Admin.Contracts/CreateTourDto.cs`
   - `src/ViajantesTurismo.Admin.Contracts/UpdateTourDto.cs`
   - `src/ViajantesTurismo.Admin.Contracts/Bookings/CreateBookingDto.cs`
   - `src/ViajantesTurismo.Catalog.Contracts/PublicMediaImageDto.cs`

### Do not generate yet

- Aggregate factory methods and state transitions.
- Business validation rules in domain models.
- Mapping that crosses bounded contexts without an explicit contract.
- EF Core query shapes and includes.
- Any media-processing, localization, or review workflow rules.

## Migration away from `Entity<TId>`

### Target shape

Introduce DDD-named interfaces before removing base classes:

```csharp
public interface IIdentified<out TId>
{
    TId Id { get; }
}

public interface IEntity<out TId> : IIdentified<TId>
{
}

public interface IAggregateRoot<out TId> : IEntity<TId>
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    void ClearDomainEvents();
}
```

The snippet shows the intended relationships only. Each top-level interface should be implemented in
its own C# file, following repository conventions.

Domain models should use `IEntity<TId>` or `IAggregateRoot<TId>` rather than `IIdentified<TId>`.
`IIdentified<TId>` remains the non-DDD primitive for read models, DTO-adjacent models, and generator
internals. Generated identity support can target all identified models, while DDD-specific behavior
targets the DDD interfaces.

Aggregate roots, not child entities, record domain events. Domain events are internal bounded-context
facts; integration events remain the externally visible contracts. Child entities can participate in
behavior, but the aggregate root records the domain event after invariants pass.

Generated support should keep that boundary explicit:

- identity/equality generation can target `IEntity<TId>`;
- domain-event collection helpers can target `IAggregateRoot<TId>`;
- non-domain models can use `IIdentified<TId>` only when identity support is useful.

### Stages

1. Add DDD identity interfaces and analyzer diagnostics.
    - No model behavior changes.
    - Keep existing `Entity<TId>` and `AggregateRoot<TId>`.

2. Make current base classes implement DDD interfaces.
    - Preserve equality semantics.
    - Add core behavior tests around default identifiers, type checks, hash behavior, aggregate-root
      domain-event recording, and domain-event clearing.

3. Add DDD rule behavior tests before generator migration.
    - Mirror the SharedKernel functional pattern tests: focused xUnit tests, explicit Arrange/Act/Assert,
      behavior traits, and no infrastructure dependencies.
    - Cover that aggregate roots can record/clear domain events.
    - Cover that plain entities expose identity/equality only and do not expose domain-event collection.
    - Cover that generated support preserves the current base-class equality semantics.

4. Opt in one small model group to generated identity.
    - Prefer a non-hot-path model with direct tests.
    - Do not mix this with EF mapping generation.

5. Migrate Admin aggregate roots one group at a time.
    - `Customer`, then `Tour`, then `Booking`/`Payment`.
    - Run Admin unit/integration tests after each group.

6. Migrate Catalog aggregate/read models only after Catalog persistence tests cover identity and EF
   materialization.

7. Remove duplicate base classes after consumers move.
   - `ViajantesTurismo.Common.BuildingBlocks.Entity<TId>` was removed after Admin consumers moved to
     SharedKernel identity interfaces and generated identity support.

### Current inventory

`Entity<TId>` consumers that must keep identity equality unchanged during migration:

| Model | Project | Current base | Notes |
| --- | --- | --- | --- |
| `Customer` | `src/ViajantesTurismo.Admin.Domain` | `IEntity<Guid>` plus generated identity support | EF key configured with `ValueGeneratedNever()`; direct entity, no domain events. |
| `Tour` | `src/ViajantesTurismo.Admin.Domain` | `IEntity<Guid>` plus generated identity support | Aggregate root by behavior; owns `Booking` collection but does not yet use SharedKernel domain events. |
| `Booking` | `src/ViajantesTurismo.Admin.Domain` | `IEntity<Guid>` plus generated identity support | Child entity inside `Tour`; EF key configured with `ValueGeneratedNever()`. |
| `Payment` | `src/ViajantesTurismo.Admin.Domain` | `IEntity<Guid>` plus generated identity support | Child entity inside `Booking`; immutable after creation; EF key configured with `ValueGeneratedNever()`. |
| `EditablePublicContent` | `src/ViajantesTurismo.Catalog.Domain` | `SharedKernel.Domain.AggregateRoot<Guid>` | Aggregate root with EF materialization constructor and explicit key generation. |
| `PublicThemeSettings` | `src/ViajantesTurismo.Catalog.Domain` | `SharedKernel.Domain.AggregateRoot<Guid>` | Singleton aggregate root with fixed `ThemeId`; EF key configured with `ValueGeneratedNever()`. |

Test-only consumers:

- `tests/SharedKernel.Domain.Tests`: `TestEntity`, `OtherTestEntity`, `TestAggregate`, and domain
  primitive/equality tests.
- `tests/ViajantesTurismo.Common.UnitTests`: common entity tests were removed with
  `ViajantesTurismo.Common.BuildingBlocks.Entity<TId>`.
- `tests/ViajantesTurismo.ArchitectureTests`: DDD convention helpers identify entity types through
  `IEntity<TId>` and allow both Admin and Catalog domain namespaces.

Docs and ADRs that still reference base classes and should be refreshed as migrations land:

- `docs/CODING_GUIDELINES.md` examples.
- `docs/DOMAIN_VALIDATION.md` factory-method examples.
- `docs/domain/EVENTS_AND_MESSAGING.md` SharedKernel domain primitive list.
- `docs/adr/20260621-split-sharedkernel-domain-and-building-blocks.md` SharedKernel ownership notes.
- `docs/adr/20251108-payment-tracking-immutable-records.md` payment example.
- `src/ViajantesTurismo.Common/README.md` base-type list now documents only remaining common base types.

### Inheritance and persistence assumptions

- Equality is currently base-class equality: same runtime type, non-default identity, then identity
  comparer equality. Generated identity support must preserve that shape before a model drops the base
  class.
- EF mappings own keys explicitly through `HasKey(...Id)` and `ValueGeneratedNever()` for persisted
  Admin and Catalog models. Migration PRs must not change key generation or migrations unless that PR
  is explicitly about persistence.
- EF materialization constructors are private and marked for tooling; keep them unchanged while only
  moving identity/equality support.
- Admin `Tour`, `Booking`, and `Payment` have aggregate-boundary assumptions. Migrate `Tour` behavior
  tests before migrating its child entities.

### Staged migration order

1. `Customer` first.
   - Lowest aggregate-boundary risk among Admin entities.
   - Add/keep Admin unit tests for identity equality and EF materialization before removing the base.
2. `Tour` aggregate root next.
   - Add behavior tests for booking collection invariants and identity equality before migration.
3. `Booking`, then `Payment`.
   - Keep child-entity changes separate from `Tour` root migration.
   - Preserve payment immutability and booking payment-status tests.
4. `EditablePublicContent`.
   - Keep domain-event behavior and EF `ValueGeneratedNever()` covered by Catalog tests.
5. `PublicThemeSettings` last.
   - Preserve fixed singleton `ThemeId` and theme replacement behavior.
6. `ViajantesTurismo.Common.BuildingBlocks.Entity<TId>` is removed in this migration.

### Guardrails

- Do not change database keys and source-generation setup in the same PR.
- Do not change equality semantics silently.
- Keep EF value generation explicit with `ValueGeneratedNever()` unless a migration deliberately
  changes key ownership.
- Add architecture or analyzer coverage before forbidding the old base classes.

## Recommended follow-up order

1. Implement identity interfaces and diagnostics.
2. Implement Vogen-like scalar value objects for one SharedKernel candidate.
3. Add explicit JSON generation after identity/value-object shape is stable.
4. Add optional EF Core mapping generation last.
