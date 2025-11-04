# Domain Validation Patterns

Domain entities enforce their own invariants through factory methods and update operations, ensuring entities are never
in an invalid state.

## Result Pattern

All domain operations that can fail return `Result` or `Result<T>`.
See [Result Pattern documentation](../src/ViajantesTurismo.Common/RESULT_PATTERN.md) for details.

## Factory Method Pattern

Entities use static factory methods instead of public constructors:

```csharp
public sealed class Tour : Entity<int>
{
    public static Result<Tour> Create(string identifier, string name, ...)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return TourErrors.EmptyIdentifier();
        
        return new Tour(identifier, name, ...);
    }
    
    private Tour(...) { }
    
    [UsedImplicitly]
    private Tour() { }
}
```

**Principles:**

- Public factory method returns `Result<T>`
- Private constructor prevents unvalidated instances
- Validate before construction
- Parameterless constructor for EF Core

## Validation Rules

### Tour Entity

Validation rules enforced in `Tour.Create()`:

- Identifier: Not empty, max 128 characters
- Name: Not empty, max 128 characters
- Dates: End > start, minimum 5 days duration
- Prices: 0 to 100,000

All constraints defined in `ContractConstants`.

### Booking Entity

Validation rules enforced in `Booking.Create()` and update operations:

**Creation:**

- Base price: Must be > 0 and <= 100,000
- Room additional cost: Must be >= 0 and <= 100,000
- Notes: Max 2000 characters
- Discount: If absolute, cannot exceed subtotal
- Final price: Must be > 0 after discount
- BikeType: Cannot be `BikeType.None` for principal or companion
- Companion: Cannot be same as principal customer

**State Transitions:**

- Pending → Confirmed: Allowed
- Pending → Cancelled: Allowed
- Confirmed → Completed: Allowed
- Confirmed → Cancelled: Allowed
- Cancelled → *: Blocked (terminal state)
- Completed → *: Blocked (terminal state)

**Updates:**

- Cannot modify Cancelled or Completed bookings
- Discount changes must keep final price > 0
- Room type changes validated for companion presence

**Payments:**

- Amount: Must be > 0
- Amount: Cannot exceed remaining balance
- Payment date: Cannot be in the future
- Payment method: Must be valid enum value (Other, CreditCard, BankTransfer, Cash, Check, PayPal)

### Customer Entity

Validation rules enforced in `Customer.Create()`:

- Personal info: FirstName, LastName not empty, max lengths
- Email: Valid format, max 256 characters
- Phone: Valid format
- Birth date: Between 18 and 120 years old

### Update Operations

Update methods return `Result`:

```csharp
public Result UpdateSchedule(DateTime newStartDate, DateTime newEndDate)
{
    newStartDate = DateTimeSanitizer.SanitizeDate(newStartDate);
    newEndDate = DateTimeSanitizer.SanitizeDate(newEndDate);

    if (newEndDate <= newStartDate)
        return TourErrors.InvalidDateRangeForUpdate();
    
    StartDate = newStartDate;
    EndDate = newEndDate;
    return Result.Ok();
}
```

## Error Classes

Each entity has an error class:

```csharp
public static class TourErrors
{
    public static Result<Tour> EmptyIdentifier() =>
        Result<Tour>.Failure(ResultStatus.Invalid,
            new ResultError("Tour identifier cannot be empty", null));
            
    public static Result EmptyIdentifierForUpdate() =>
        Result.Failure(ResultStatus.Invalid,
            new ResultError("Tour identifier cannot be empty", null));
}
```

Provide both `Result<T>` (factory) and `Result` (updates) versions.

## Contract Constants

Validation constants live in `ContractConstants.cs`:

```csharp
public static class ContractConstants
{
    public const int MaxNameLength = 128;
    public const int MinimumTourDurationDays = 5;
    public const double MaxPrice = 100_000;
}
```

## API Integration

**Tour Creation:**
```csharp
var result = Tour.Create(dto.Identifier, dto.Name, ...);
if (!result.IsSuccess)
    return result.ToValidationProblem();

var tour = result.Value;
await unitOfWork.SaveEntities(ct);
```

**Booking Creation:**

```csharp
var result = tour.AddBooking(
    principalCustomerId,
    principalBikeType,
    companionCustomerId,
    companionBikeType,
    roomType,
    discountType,
    discountAmount,
    discountReason,
    notes);
    
if (!result.IsSuccess)
    return result.ToValidationProblem();

var booking = result.Value;
await unitOfWork.SaveEntities(ct);
```

**Payment Recording:**

```csharp
var result = booking.RecordPayment(
    amount,
    paymentDate,
    method,
    TimeProvider.System,
    referenceNumber,
    notes);
    
if (!result.IsSuccess)
    return result.ToValidationProblem();

await unitOfWork.SaveEntities(ct);
```

## Testing

```csharp
[Then(@"the operation should fail")]
public void ThenOperationShouldFail()
{
    var (isSuccess, errorDetail) = _result switch
    {
        Result<Tour> tr => (tr.IsSuccess, tr.ErrorDetails?.Detail),
        Result r => (r.IsSuccess, r.ErrorDetails?.Detail),
        _ => throw new InvalidOperationException("Unexpected result type")
    };
    
    Assert.False(isSuccess);
    Assert.Contains("expected error", errorDetail!);
}
```
