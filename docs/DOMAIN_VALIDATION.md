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

```csharp
var result = Tour.Create(dto.Identifier, dto.Name, ...);
if (!result.IsSuccess)
    return result.ToValidationProblem();

var tour = result.Value;
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
