# C# Coding Guidelines

Coding standards and conventions for C# development in ViajantesTurismo projects.

## Project Configuration

### Language & Framework

- **Target Framework:** .NET 10.0 (`net10.0`)
- **Language Version:** Preview (latest C# features)
- **Nullable Reference Types:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Warnings as Errors:** Enabled (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Code Analysis:** All rules enabled, latest level (`<AnalysisMode>All</AnalysisMode>`)

### EditorConfig

All projects use `.editorconfig` for consistent formatting. Key settings:

- **Indentation:** 4 spaces, no tabs
- **Line Endings:** CRLF (Windows)
- **Max Line Length:** 200 characters
- **Final Newline:** Required in all files

## Naming Conventions

### Casing Rules

| Element               | Casing                     | Example                              |
|-----------------------|----------------------------|--------------------------------------|
| Namespace             | PascalCase                 | `ViajantesTurismo.Admin.Domain`      |
| Class, Record, Struct | PascalCase                 | `Tour`, `BookingCustomer`            |
| Interface             | PascalCase with `I` prefix | `ITourRepository`, `IUnitOfWork`     |
| Method                | PascalCase                 | `Create()`, `UpdateSchedule()`       |
| Property              | PascalCase                 | `Identifier`, `CurrentCustomerCount` |
| Public Field          | PascalCase                 | `MaxPrice` (constants)               |
| Private Field         | camelCase with `_` prefix  | `_bookings`, `_includedServices`     |
| Parameter             | camelCase                  | `identifier`, `startDate`            |
| Local Variable        | camelCase                  | `result`, `tour`                     |
| Const                 | PascalCase                 | `MaxNameLength`                      |

### Specific Conventions

**Asynchronous methods:** Do not use the `Async` suffix. Name methods for their
intent whether they are synchronous or asynchronous.

**Error Classes:** Suffix with `Errors`

```csharp
public static class TourErrors { }
public static class BookingErrors { }
```

**Domain Events:** Past tense + `DomainEvent` suffix

```csharp
public sealed record TourCreatedDomainEvent(Guid TourId);
public sealed record BookingConfirmedDomainEvent(Guid BookingId);
```

**Integration Events:** Past tense + `IntegrationEvent` suffix

```csharp
public sealed record TourCreatedIntegrationEvent(Guid TourId, string Name);
```

**Value Objects:** Descriptive noun, often compound

```csharp
public sealed record DateRange(DateTime StartDate, DateTime EndDate);
public sealed record TourPricing(Money BasePrice, Money RoomSupplement, Money BikePrice);
```

## File Organization

### Namespace Declaration

Use file-scoped namespaces (enforced by EditorConfig):

```csharp
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

public sealed class Tour : Entity<Guid> { }
```

### Using Directives

- **Placement:** Outside namespace (enforced by EditorConfig)
- **Order:** System → third-party → project namespaces
- **Implicit Usings:** Enabled globally

### File Structure

Order: Using directives → Namespace → XML docs → Type declaration → Private fields →
Constructors (private first) → Public properties → Public methods (factory first) → Private methods

## Code Style

### Expression Bodies

Use for simple properties and lambdas, not for methods or constructors:

```csharp
public int AvailableSpots => Capacity.MaxCustomers - CurrentCustomerCount;
public bool IsFullyBooked => CurrentCustomerCount >= Capacity.MaxCustomers;
```

### Pattern Matching

Prefer pattern matching over explicit casts. Use switch expressions where appropriate:

```csharp
if (obj is Tour tour) { return tour.Identifier; }

var message = status switch
{
    BookingStatus.Pending => "Awaiting confirmation",
    BookingStatus.Confirmed => "Booking confirmed",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};
```

### Null Handling

- **Nullable Reference Types:** Enabled globally, use `?` for nullable types
- **Null-coalescing:** Prefer `??` and `??=` operators
- **Null-conditional:** Use `?.` for null propagation

### Guard Clauses at Public Boundaries

Guard public entry points and fail fast for invalid caller input.

- Use `ArgumentNullException.ThrowIfNull(value);` for required references
- Use `ArgumentException.ThrowIfNullOrWhiteSpace(value);` for required strings
- Use `ArgumentOutOfRangeException` for invalid ranges

Guard once at the boundary. Use `Result` for expected business validation.

```csharp
public TourCommandHandler(ITourRepository repository, IUnitOfWork unitOfWork)
{
    ArgumentNullException.ThrowIfNull(repository);
    ArgumentNullException.ThrowIfNull(unitOfWork);

    _repository = repository;
    _unitOfWork = unitOfWork;
}

public static string NormalizeIdentifier(string value)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return value.Trim().ToUpperInvariant();
}
```

### Primary Constructors

Prefer primary constructors for simple classes:

```csharp
public sealed record Money(decimal Amount, Currency Currency);
```

### Collection Expressions

Use collection expressions (`[]`) for initialization:

```csharp
private readonly List<Booking> _bookings = [];
private string[] _services = ["Hotel", "Breakfast"];
_includedServices = [.. includedServices]; // Spreading
```

### Line Breaking

Keep arguments on one line if they fit (200 char limit). When breaking, put **one argument per line**:

```csharp
// ✅ Single line
var result = Tour.Create(identifier, name, startDate, endDate);

// ✅ Multi-line: one argument per line
var booking = tour.AddBooking(
    customerId,
    bikeType,
    roomType);
```

### Braces

Always use braces for control flow statements (EditorConfig enforces).
Keep the opening brace on the same line as the declaration or control-flow statement unless
the local formatter or surrounding code in that file clearly requires a different layout.

### Type Declarations

- **Explicit types** for domain objects (clarity over brevity): `Tour tour = result.Value;`
- **`var`** for obvious types (LINQ, constructors): `var bookings = _bookings.ToList();`

## Documentation

### XML Documentation

All public APIs must have XML documentation.
Required tags: `<summary>`, `<param>`, `<returns>`, `<remarks>` for aggregates.

```csharp
/// <summary>Creates a new <see cref="Tour"/> with validation.</summary>
/// <param name="identifier">Unique business identifier.</param>
/// <returns>Result containing the tour or validation error.</returns>
public static Result<Tour> Create(string identifier, string name) { }
```

### Code Comments

**When to comment:** Complex business logic, workarounds, ADR references.
**When NOT to comment:** Self-explanatory code, restating the obvious.

## Immutability

- **Value Objects:** Always immutable (`record` or readonly struct)
- **Entities:** Mutable via methods returning `Result`
- **Collections:** Store as `List<T>`, expose as `IReadOnlyList<T>`
- **DTOs:** Use `init` and `required` for immutable initialization

```csharp
public sealed record Money(decimal Amount, Currency Currency);

public sealed class Tour : Entity<Guid>
{
    private readonly List<Booking> _bookings = [];
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();
}

public sealed class TourDto
{
    public required string Identifier { get; init; }
}
```

## Error Handling

Use the Result pattern for expected failures. Throw exceptions only for programmer errors,
system failures, or invariant violations. See [DOMAIN_VALIDATION.md](DOMAIN_VALIDATION.md) for details.

```csharp
// ✅ Return Result for validation
public static Result<Tour> Create(string identifier, string name)
{
    if (string.IsNullOrWhiteSpace(identifier))
        return TourErrors.EmptyIdentifier();
    return new Tour(identifier, name);
}

// ✅ Throw for invariant violations
if (_bookings.Count > Capacity.MaxCustomers)
    throw new InvalidOperationException("Invariant violated");
```

Use argument exceptions for invalid caller input and `Result` for expected business validation.

## Testing

Keep methods focused, avoid static dependencies, use interfaces for repositories.
See [TEST_GUIDELINES.md](TEST_GUIDELINES.md).

## Native AOT Compatibility

### Project Configuration

Library projects should enable AOT analysis to catch compatibility issues at compile time:

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

API projects should additionally enable the Request Delegate Generator:

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
  <EnableRequestDelegateGenerator>true</EnableRequestDelegateGenerator>
</PropertyGroup>
```

**Note:** `IsAotCompatible=true` implicitly sets `IsTrimmable=true`, enabling both trim and AOT analyzers.

### JSON Serialization

Use source-generated JSON serializers instead of reflection-based serialization:

```csharp
// ✅ CORRECT - Source-generated serializer
[JsonSerializable(typeof(TourDto))]
[JsonSerializable(typeof(CustomerDto))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

// Register in Program.cs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// ❌ WRONG - Reflection-based serialization
JsonSerializer.Serialize(dto); // Uses reflection at runtime
```

**Note:** Only list top-level types in `[JsonSerializable]` attributes.
The source generator automatically discovers and generates serializers for member types.

### Validation Patterns

Avoid custom validation attributes that use reflection. Instead, implement `IValidatableObject`:

```csharp
// ✅ CORRECT - IValidatableObject pattern
public record TourDto : IValidatableObject
{
    public required int Duration { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Duration < TourConstraints.MinDuration)
        {
            yield return new ValidationResult(
                $"Duration must be at least {TourConstraints.MinDuration} days.",
                [nameof(Duration)]);
        }
    }
}

// ❌ WRONG - Custom validation attribute (uses reflection)
[TourDurationRange]
public required int Duration { get; init; }
```

### CreateSlimBuilder

For AOT-optimized API applications, use `CreateSlimBuilder` instead of `CreateBuilder`:

```csharp
// ✅ CORRECT - Slim builder for AOT
var builder = WebApplication.CreateSlimBuilder(args);

// ❌ WRONG - Full builder includes AOT-incompatible features
var builder = WebApplication.CreateBuilder(args);
```

`CreateSlimBuilder` excludes features incompatible with AOT
(hosting startup assemblies, IIS integration, EventLog logging, etc.).

### Known Limitations

| Component                              | AOT Status        | Notes                                                    |
| -------------------------------------- | ----------------- | -------------------------------------------------------- |
| Common, Contracts, Domain, Application | ✅ Compatible     | `IsAotCompatible=true` enabled                           |
| ApiService                             | ✅ Compatible     | Uses `CreateSlimBuilder`, JSON source generators         |
| Infrastructure (EF Core)               | ⚠️ Partial        | Compiled models generated, but DbContext blocks full AOT |
| Web (Blazor Server)                    | ❌ Not Compatible | Blazor Server not supported for Native AOT               |

## Performance

- Use `Span<T>` and `ReadOnlySpan<T>` for stack allocations
- Use `async`/`await` for I/O-bound operations
- Don't use `async void` except for event handlers
- `ConfigureAwait(false)` is NOT needed (InvariantGlobalization enabled)

## Related Documentation

- **[Domain Validation](DOMAIN_VALIDATION.md)** — Domain modeling patterns, factory methods, Result pattern
- **[Test Guidelines](TEST_GUIDELINES.md)** — Testing patterns and BDD scenarios
- **[Code Quality](CODE_QUALITY.md)** — Linters, formatters, and tooling configuration
- **[Architecture Decisions](ARCHITECTURE_DECISIONS.md)** — Core architectural patterns and decisions
