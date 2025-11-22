# ViajantesTurismo.Common

Shared types and patterns used across all projects.

## Contents

### Functional Programming Patterns

**`Result`** and **`Result<T>`**: Railway-oriented error handling for operations that can fail.

```csharp
// Success/Failure without value
Result result = Result.Success();
Result error = Result.Failure(ResultStatus.Invalid, new ResultError("Error message", null));

// Success/Failure with value
Result<Tour> success = Result<Tour>.Success(tour);
Result<Tour> failure = Result<Tour>.Failure(ResultStatus.NotFound, new ResultError("Tour not found", null));

// Pattern matching
if (result.IsSuccess) { /* ... */ }
if (result.IsFailure) { /* use result.ErrorDetails */ }
```

**`Option<T>`**: Type-safe handling of optional values (may or may not be present).

```csharp
// Create with value
Option<Customer> some = Option<Customer>.Of(customer);

// Create empty
Option<Customer> none = Option<Customer>.Empty();

// From nullable
Option<Customer> maybe = Option<Customer>.FromNullable(nullableCustomer);

// Check and use
if (option.HasValue)
{
    var customer = option.Value;
}
```

**Benefits**:

- Explicit error handling (Result)
- Type-safe optional values (Option)
- No exceptions for business rule violations
- Compiler-enforced checking

**Documentation**: See [FUNCTIONAL_PATTERNS.md](FUNCTIONAL_PATTERNS.md) for comprehensive usage guide.

### Enumerations

See [Domain Glossary](../../docs/domain/GLOSSARY.md) for complete enum definitions and values.

Common enums include: BedType, BikeType, BookingStatus, Currency, Gender, PaymentStatus, RoomType.

### Base Types

- **Entity\<TId\>**: Base class for all domain entities with identity
- **ValueObject**: Base class for immutable value objects compared by their attributes

#### ValueObject

Value objects are immutable objects defined by their attributes rather than identity.

```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}

// Two addresses with the same values are equal
var address1 = new Address("123 Main St", "Springfield", "12345");
var address2 = new Address("123 Main St", "Springfield", "12345");
address1 == address2; // true
```

**Characteristics**:

- Immutable: State cannot change after creation
- Equality by value: Compared by their attributes, not identity
- No identity: Don't have unique identifiers
- Side-effect free: Operations return new instances

## Dependencies

Zero external dependencies - only .NET BCL.
