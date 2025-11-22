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

Use file-scoped namespaces (required by EditorConfig):

```csharp
// ✅ CORRECT
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

public sealed class Tour : Entity<Guid>
{
    // ...
}
```

```csharp
// ❌ WRONG - Block-scoped namespace
namespace ViajantesTurismo.Admin.Domain.Tours
{
    public sealed class Tour : Entity<Guid>
    {
        // ...
    }
}
```

### Using Directives

- **Placement:** Outside namespace (enforced by EditorConfig)
- **Order:** System namespaces first, then third-party, then project namespaces
- **Implicit Usings:** Enabled globally, don't repeat common system namespaces

```csharp
using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;
```

### File Structure

1. Using directives
2. Namespace declaration
3. XML documentation comment
4. Class/record/interface declaration
5. Private fields
6. Constructors (private first for factory pattern)
7. Public properties
8. Public methods (factory methods first)
9. Private methods

**Example:**

```csharp
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a tour entity with schedule, pricing, and booking management.
/// </summary>
public sealed class Tour : Entity<Guid>
{
    // Private fields
    private readonly List<Booking> _bookings = [];

    // Private constructor (for factory pattern)
    private Tour(string identifier, string name)
    {
        Identifier = identifier;
        Name = name;
    }

    // Public properties
    public string Identifier { get; private set; }
    public string Name { get; private set; }

    // Public factory method
    public static Result<Tour> Create(string identifier, string name)
    {
        // Validation and creation logic
    }

    // Public methods
    public Result UpdateName(string newName)
    {
        // Update logic
    }

    // Private methods
    private void ValidateInvariants()
    {
        // Internal validation
    }
}
```

## Code Style

### Expression Bodies

Use expression bodies for simple properties and lambdas:

```csharp
// ✅ Properties
public int AvailableSpots => Capacity.MaxCustomers - CurrentCustomerCount;
public bool IsFullyBooked => CurrentCustomerCount >= Capacity.MaxCustomers;

// ✅ Lambdas
.Where(b => b.Status == BookingStatus.Confirmed)
.Select(b => b.ToDto())

// ❌ Not for methods or constructors
public Result UpdateName(string name) => /* ... */; // DON'T
```

### Pattern Matching

Prefer pattern matching over explicit casts:

```csharp
// ✅ CORRECT
if (obj is Tour tour)
{
    return tour.Identifier;
}

// ❌ WRONG
if (obj is Tour)
{
    var tour = (Tour)obj;
    return tour.Identifier;
}
```

Use switch expressions where appropriate:

```csharp
// ✅ CORRECT
var message = status switch
{
    BookingStatus.Pending => "Awaiting confirmation",
    BookingStatus.Confirmed => "Booking confirmed",
    BookingStatus.Cancelled => "Booking cancelled",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};
```

### Null Handling

- **Nullable Reference Types:** Enabled globally, use `?` for nullable types
- **Null-coalescing:** Prefer `??` and `??=` operators
- **Null-conditional:** Use `?.` for null propagation

```csharp
// ✅ Nullable parameter
public Result UpdateNotes(string? notes)
{
    Notes = notes ?? string.Empty;
}

// ✅ Null-conditional
var price = booking?.FinalPrice ?? 0;

// ✅ Null-coalescing assignment
_includedServices ??= [];
```

### Primary Constructors

Prefer primary constructors for simple classes (EditorConfig warning):

```csharp
// ✅ CORRECT - Primary constructor
public sealed record Money(decimal Amount, Currency Currency);

// ✅ CORRECT - For classes needing validation
public sealed class TourPricing(Money basePrice, Money roomSupplement)
{
    public Money BasePrice { get; } = basePrice;
    public Money RoomSupplement { get; } = roomSupplement;
}
```

### Collection Expressions

Use collection expressions (`[]`) for inline initialization:

```csharp
// ✅ CORRECT
private readonly List<Booking> _bookings = [];
private string[] _services = ["Hotel", "Breakfast", "Bike Rental"];

// ✅ Spreading
_includedServices = [.. includedServices];

// ❌ WRONG - Old syntax
private readonly List<Booking> _bookings = new List<Booking>();
private string[] _services = new[] { "Hotel", "Breakfast" };
```

### Method Braces

Always use braces for control flow statements (EditorConfig enforces):

```csharp
// ✅ CORRECT
if (count > 0)
{
    return Result.Success();
}

// ❌ WRONG - No braces
if (count > 0)
    return Result.Success();
```

### Implicit vs Explicit Types

- **Prefer explicit types** for domain objects (clarity over brevity)
- **Use `var`** for obvious types (LINQ, constructors)

```csharp
// ✅ CORRECT - Explicit domain types
Tour tour = result.Value;
BookingStatus status = BookingStatus.Confirmed;

// ✅ CORRECT - var for obvious types
var bookings = _bookings.Where(b => b.Status == status).ToList();
var customer = new Customer(name, email);

// ❌ WRONG - var hides domain types
var tour = result.Value; // What type is this?
```

## Documentation

### XML Documentation

All public APIs must have XML documentation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`):

```csharp
/// <summary>
/// Creates a new instance of the <see cref="Tour"/> class with validation.
/// </summary>
/// <param name="identifier">Unique business identifier for the tour.</param>
/// <param name="name">Display name of the tour.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the created tour on success,
/// or an error on validation failure.
/// </returns>
public static Result<Tour> Create(string identifier, string name)
{
    // ...
}
```

**Required Tags:**

- `<summary>` - Brief description
- `<param>` - For each parameter
- `<returns>` - For methods returning values
- `<exception>` - For thrown exceptions (rare with Result pattern)
- `<remarks>` - Additional context, invariants, or aggregate documentation

**Example with remarks:**

```csharp
/// <summary>
/// Represents a tour entity with schedule, pricing, and booking management.
/// </summary>
/// <remarks>
/// <para><strong>AGGREGATE ROOT:</strong> Tour is the aggregate root for the Tour-Booking aggregate.</para>
/// <para>All Booking entities must be created and modified through Tour methods to maintain consistency.</para>
/// <para>Tour enforces business rules and invariants for all bookings within its aggregate boundary.</para>
/// </remarks>
public sealed class Tour : Entity<Guid>
{
    // ...
}
```

### Code Comments

**When to Comment:**

- Complex business logic that isn't obvious from code
- Workarounds for known limitations
- References to ADRs or external documentation

**When NOT to Comment:**

- Self-explanatory code
- Restating the code in English
- Documenting domain concepts (use DOMAIN_VALIDATION.md instead)

```csharp
// ✅ GOOD - Explains non-obvious business rule
// PaymentStatus is calculated from payments, not stored
// See ADR-016: PaymentStatus as Calculated Property
public PaymentStatus PaymentStatus => /* ... */;

// ❌ BAD - Restates the code
// Get the identifier
public string Identifier { get; private set; }
```

## Immutability

### Prefer Immutability

- **Value Objects:** Always immutable (use `record` or readonly structs)
- **Entities:** Mutable state, but expose via methods returning `Result`
- **Collections:** Expose as `IReadOnlyList<T>`, store as `List<T>`

```csharp
// ✅ Immutable value object
public sealed record Money(decimal Amount, Currency Currency);

// ✅ Entity with controlled mutation
public sealed class Tour : Entity<Guid>
{
    private readonly List<Booking> _bookings = [];

    // Immutable public interface
    public IReadOnlyList<Booking> Bookings => _bookings.AsReadOnly();

    // Mutation through validated methods
    public Result<Booking> AddBooking(/* ... */)
    {
        // Validation
        var booking = new Booking(/* ... */);
        _bookings.Add(booking);
        return Result<Booking>.Success(booking);
    }
}
```

### Init-Only Properties

Use `init` for immutable objects initialized via object initializers:

```csharp
public sealed class TourDto
{
    public required string Identifier { get; init; }
    public required string Name { get; init; }
    public required DateTime StartDate { get; init; }
}

// Usage
var dto = new TourDto
{
    Identifier = "TOUR-001",
    Name = "Alps Adventure",
    StartDate = DateTime.UtcNow
};
```

## Error Handling

### Use Result Pattern

Never throw exceptions for expected validation failures. See [DOMAIN_VALIDATION.md](DOMAIN_VALIDATION.md) for
comprehensive Result pattern usage.

```csharp
// ✅ CORRECT - Return Result
public static Result<Tour> Create(string identifier, string name)
{
    if (string.IsNullOrWhiteSpace(identifier))
        return TourErrors.EmptyIdentifier();

    return new Tour(identifier, name);
}

// ❌ WRONG - Throw exception
public static Tour Create(string identifier, string name)
{
    if (string.IsNullOrWhiteSpace(identifier))
        throw new ArgumentException("Identifier cannot be empty");

    return new Tour(identifier, name);
}
```

### When to Throw Exceptions

Only for truly exceptional conditions:

- Programmer errors (null reference to non-nullable)
- System failures (database unavailable, out of memory)
- Invariant violations that should never occur

```csharp
// ✅ CORRECT - Invariant violation
if (_bookings.Count > Capacity.MaxCustomers)
    throw new InvalidOperationException("Booking count exceeded maximum capacity. This should never happen.");
```

## Testing Considerations

### Testable Design

- Keep methods focused and single-purpose
- Avoid static dependencies (use dependency injection)
- Use interfaces for repositories and external dependencies

```csharp
// ✅ Testable - Interface dependency
public sealed class CreateTourHandler(ITourRepository repository)
{
    public async Task<Result<Tour>> Handle(CreateTourCommand command, CancellationToken ct)
    {
        var result = Tour.Create(command.Identifier, command.Name, /* ... */);
        if (!result.IsSuccess)
            return result;

        await repository.AddAsync(result.Value, ct);
        return result;
    }
}
```

## Performance

### Avoid Allocations

- Use `Span<T>` and `ReadOnlySpan<T>` for stack allocations
- Reuse collections where appropriate
- Use `ValueTask` for hot paths

### Async/Await

- Use `async`/`await` for I/O-bound operations
- Don't use `async void` except for event handlers
- Use `ConfigureAwait(false)` is NOT needed (InvariantGlobalization enabled)

```csharp
// ✅ CORRECT
public async Task<Result<Tour>> Handle(CreateTourCommand command, CancellationToken ct)
{
    await repository.AddAsync(tour, ct);
    return Result<Tour>.Success(tour);
}
```

## Related Documentation

- **[Domain Validation](DOMAIN_VALIDATION.md)** — Domain modeling patterns, factory methods, Result pattern
- **[Test Guidelines](TEST_GUIDELINES.md)** — Testing patterns and BDD scenarios
- **[Code Quality](CODE_QUALITY.md)** — Linters, formatters, and tooling configuration
- **[Architecture Decisions](ARCHITECTURE_DECISIONS.md)** — Core architectural patterns and decisions
