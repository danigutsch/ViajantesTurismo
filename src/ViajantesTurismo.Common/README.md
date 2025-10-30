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

- **BedType**: Single, Double, Twin, Queen, King
- **BikeType**: Regular, Electric
- **BookingStatus**: Pending, Confirmed, Cancelled, Completed
- **Currency**: UsDollar, Euro, BritishPound, CanadianDollar, AustralianDollar, SwissFranc
- **Gender**: Male, Female, Other, PreferNotToSay
- **PaymentStatus**: Pending, PartiallyPaid, FullyPaid, Refunded
- **RoomType**: Single, Double, Twin, Triple, Suite

### Base Types

- **Entity<TId>**: Base class for all domain entities with identity

## Dependencies

Zero external dependencies - only .NET BCL.

