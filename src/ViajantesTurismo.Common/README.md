# ViajantesTurismo.Common

Shared types and patterns used across all projects.

## Contents

### Result Pattern

**`Result`** and **`Result<T>`**: Railway-oriented error handling.

```csharp
// Success/Failure without value
Result result = Result.Success();
Result error = Result.Failure("Error message");

// Success/Failure with value
Result<Tour> success = Result<Tour>.Success(tour);
Result<Tour> failure = Result<Tour>.Failure("Tour not found");

// Pattern matching
if (result.IsSuccess) { /* ... */ }
if (result.IsFailure) { /* use result.Error */ }
```

**Benefits**:

- Explicit error handling
- No exceptions for business rule violations
- Compiler-enforced error checking

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
