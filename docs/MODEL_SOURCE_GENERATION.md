# Configurable Model Source Generation

This design note defines the option shape for configurable model generation and records the first
repository audit.

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

Value-object generation supports explicitly annotated `readonly partial record struct` types. Assembly
defaults can enable optional parsing, JSON, and EF Core generation for those annotated value objects,
but the value object itself still needs `UnderlyingType` on the type. The generated shape is
intentionally scalar-only and additive:

- `Value` exposes the underlying scalar.
- `Create` and `TryCreate` validate before construction.
- `Parse` and `TryParse` are emitted only when `Parsing = true` or when the selected template needs
  parsing.
- `static partial void ValidateValue(T value, ref bool isValid)` lets the owning type add explicit
  validation without generated business rules.
- `ToString` formats using invariant culture for numeric and date values.

Supported underlying scalar types are `string`, `Guid`, `int`, `decimal`, and `DateOnly`.

```csharp
[assembly: GenerateModelSupportDefaults(
    Identity = true,
    ValueObjectParsing = true,
    ValueObjectJson = true,
    ValueObjectEfCore = true)]
```

Per-type attributes override assembly defaults. Defaults stay conservative: identity support applies
only to `IIdentified<TId>` models, and value-object defaults apply only to types already annotated
with `GenerateValueObjectAttribute` and an explicit `UnderlyingType`.

### Option groups

| Option | Default | Dependency | Notes |
| --- | --- | --- | --- |
| `Identity` | Off | None | Emits equality/hash helpers for `IIdentified<TId>` models. |
| `ValueObject` | Off | None | Emits scalar wrappers, `TryCreate`, parse/format helpers, and converters. |
| `Validation` | Off | None | Emits guard plumbing from explicit validation attributes only. |
| `Json` | Off | `System.Text.Json` | Emits a `JsonConverter` only when requested. |
| `EfCore` | Off | `Microsoft.EntityFrameworkCore` | Emits a value converter only when requested and EF Core is referenced. |
| `DtoConversion` | Off | None | Deferred; only when source and target are explicit and same-boundary safe. |

DDD/core generation is dependency-free and available when explicitly enabled for an opted-in model.
Cross-cutting support that depends on packages, such as EF Core or `System.Text.Json`, must either be
explicitly enabled or enabled only when the dependency exists and assembly defaults requested it.

### Optional value-object integrations

`Json = true` emits a nested `JsonConverter` for the generated value object. The converter reads and
writes the scalar value directly, avoiding reflection-heavy paths. API-version value objects write the
route segment form, such as `v1`, while still accepting numeric JSON values for tolerant reads.

`EfCore = true` emits a nested `EfCoreValueConverter` and a top-level EF Core property-builder
extension only when EF Core value-converter and property-builder types are available to the consuming
compilation. The converter maps the generated value object to its scalar `Value` and reconstructs it
through `Create`, so validation remains centralized.

### Specialized value-object templates

Templates are opt-in technical invariants for recurring protocol values. They must not encode
application-specific business policy.

| Template | Underlying type | Generated behavior |
| --- | --- | --- |
| `ApiVersion` | `int` | Positive version values, `v1` route-segment formatting, parsing with optional `v` prefix, comparison, JSON route-segment writing. |
| `NonEmptyString` | `string` | Rejects null, empty, and whitespace values. |
| `Slug` | `string` | Requires lowercase letters, digits, and hyphens without leading or trailing hyphens. |
| `StronglyTypedId` | `Guid` or `int` | Rejects default identifiers. |
| `IsoCode` | `string` | Requires two- or three-letter alphabetic codes. |

### Diagnostics

The generator should fail fast with diagnostics for unsupported combinations:

- EF Core generation requested but EF Core is not referenced.
- JSON generation requested but `System.Text.Json` is not referenced.
- Identity generation requested on a type without a readable identifier.
- Value-object parsing requested without an underlying scalar type.
- Value-object generation requested on a non-`readonly partial record struct`.
- Value-object generation requested with an unsupported scalar type or incompatible template.
- Value-object generation requested on a type with explicit constructors that could bypass generated
  validation.
- Value-object generation requested on a type that already declares a member generated by the value-
  object generator, such as `Value`, `Create`, `TryCreate`, or selected integration/template members.
- DTO conversion requested across disallowed project boundaries.

Diagnostics should name the attribute option, the affected type, and the smallest corrective action.

### Generated output rules

- Deterministic file names are based on the type metadata name, including namespace:
  `<Namespace>.<TypeName>.ModelSupport.g.cs`, `<Namespace>.<TypeName>.ValueObject.g.cs`,
  `<Namespace>.<TypeName>.EfCore.g.cs`, `<Namespace>.<TypeName>.Json.g.cs`.
- Generated code under the target type namespace.
- Public generated APIs documented with XML comments.
- No generated persistence behavior should replace explicit domain invariants.
- Tests should cover option precedence, diagnostics, and stable generated text.

## Repository audit

### High payoff, low risk

1. Identity/equality boilerplate currently centralized by base classes:
   - `src/SharedKernel/SharedKernel.Domain/AggregateRoot.cs`
   - `src/SharedKernel/SharedKernel.Domain/Entity.cs`
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
   - `src/ViajantesTurismo.Admin.Contracts.Application/CreateTourDto.cs`
   - `src/ViajantesTurismo.Admin.Contracts.Application/UpdateTourDto.cs`
   - `src/ViajantesTurismo.Admin.Contracts.Application/Bookings/CreateBookingDto.cs`
   - `src/ViajantesTurismo.Catalog.Contracts.Application/PublicMediaImageDto.cs`

### Do not generate yet

- Aggregate factory methods and state transitions.
- Business validation rules in domain models.
- Mapping that crosses bounded contexts without an explicit contract.
- EF Core query shapes and includes.
- Any media-processing, localization, or review workflow rules.

## Identity interfaces

`SharedKernel.BuildingBlocks.IIdentified<TId>` exposes a stable `Id` for any identified model.

`SharedKernel.Domain.IEntity<TId>` extends `IIdentified<TId>` for domain entities.
`SharedKernel.Domain.IAggregateRoot<TId>` extends `IEntity<TId>` and the non-generic
`IAggregateRoot`, which exposes domain-event retrieval and clearing.

Domain models implement `IEntity<TId>` or `IAggregateRoot<TId>`. Non-domain models may implement
`IIdentified<TId>` when generated identity support is useful. Identity generation targets opted-in
`IIdentified<TId>` implementations.

## Recommended follow-up order

1. Implement identity interfaces and diagnostics.
2. Implement Vogen-like scalar value objects for one SharedKernel candidate.
3. Add explicit JSON generation after identity/value-object shape is stable.
4. Add optional EF Core mapping generation last.
