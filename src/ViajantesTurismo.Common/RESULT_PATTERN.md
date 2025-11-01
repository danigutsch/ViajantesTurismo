# Result Pattern Implementation

## Core Types

### Result (Non-Generic)

Used for operations that don't return a value:

```csharp
public readonly struct Result
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public ResultStatus Status { get; }
    public ResultError? ErrorDetails { get; }
    
    public static Result Success() { }
    public static Result Failure(ResultStatus status, ResultError error) { }
}
```

### Result<T> (Generic)

Used for operations that return a value on success:

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public T Value { get; }
    public ResultError? ErrorDetails { get; }
    
    public static Result<T> Success(T value) { }
    public static Result<T> Failure(ResultStatus status, ResultError error) { }
}
```

### ResultError

Represents error information:

```csharp
public record ResultError(
    string Detail,
    Dictionary<string, string[]>? ValidationErrors);
```

### ResultStatus

Indicates the type of failure:

```csharp
public enum ResultStatus
{
    Success,
    Invalid,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Error
}
```

## Important Notes

### Result is a Struct

Both `Result` and `Result<T>` are **readonly structs**, not classes. This has important implications:

**Cannot use `as` operator for casting:**

```csharp
// ❌ WRONG - Cannot use 'as' with struct types
var result = _result as Result;

// ✅ CORRECT - Use pattern matching
if (_result is Result result) { }
```

**Use pattern matching for type checking:**

```csharp
private object? _result;

if (_result is Result nonGenericResult)
{
    Assert.False(nonGenericResult.IsSuccess);
}
else if (_result is Result<Tour> tourResult)
{
    Assert.False(tourResult.IsSuccess);
}
```

### Accessing Error Messages

Error details are nested within the `ErrorDetails` property:

**Structure:**

- `Result.ErrorDetails` → `ResultError?`
- `ResultError.Detail` → `string`

**Correct access:**

```csharp
// ✅ CORRECT
if (!result.IsSuccess)
{
    string message = result.ErrorDetails!.Detail;
}

// ❌ WRONG - Result doesn't have Detail property
string message = result.Detail;
```

## Usage Patterns

### Domain Operations

**Factory Methods:**

```csharp
public static Result<Tour> Create(string identifier, string name, ...)
{
    if (string.IsNullOrWhiteSpace(identifier))
    {
        return Result<Tour>.Failure(
            ResultStatus.Invalid,
            new ResultError("Tour identifier cannot be empty", null));
    }
    
    return Result<Tour>.Success(new Tour(identifier, name, ...));
}
```

**Update Operations:**

```csharp
public Result UpdateSchedule(DateTime startDate, DateTime endDate)
{
    if (endDate <= startDate)
    {
        return Result.Failure(
            ResultStatus.Invalid,
            new ResultError("End date must be after start date", null));
    }
    
    StartDate = startDate;
    EndDate = endDate;
    
    return Result.Success();
}
```

### API Endpoints

**Creating Entities:**

```csharp
var result = Tour.Create(dto.Identifier, dto.Name, ...);

if (!result.IsSuccess)
{
    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["Tour"] = [result.ErrorDetails!.Detail]
    });
}

var tour = result.Value;
await unitOfWork.SaveChangesAsync(ct);

return TypedResults.Created($"/tours/{tour.Id}", tour.ToGetDto());
```

**Updating Entities:**

```csharp
var scheduleResult = tour.UpdateSchedule(dto.StartDate, dto.EndDate);

if (!scheduleResult.IsSuccess)
{
    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["Schedule"] = [scheduleResult.ErrorDetails!.Detail]
    });
}

await unitOfWork.SaveChangesAsync(ct);
return TypedResults.Ok(tour.ToGetDto());
```

### Testing

**Handling Both Result Types:**

```csharp
private object? _result;

[When(@"I perform some operation")]
public void WhenIPerformOperation()
{
    _result = someEntity.SomeOperation();
}

[Then(@"the operation should fail")]
public void ThenOperationShouldFail()
{
    Assert.NotNull(_result);
    
    if (_result is Result result)
    {
        Assert.False(result.IsSuccess);
        Assert.Contains("expected error", result.ErrorDetails!.Detail);
    }
    else if (_result is Result<Tour> tourResult)
    {
        Assert.False(tourResult.IsSuccess);
        Assert.Contains("expected error", tourResult.ErrorDetails!.Detail);
    }
    else
    {
        Assert.Fail("Unexpected result type");
    }
}
```

**Success Scenarios:**

```csharp
[Then(@"the operation should succeed")]
public void ThenOperationShouldSucceed()
{
    if (_result is Result<Tour> tourResult)
    {
        Assert.True(tourResult.IsSuccess);
        Assert.NotNull(tourResult.Value);
    }
}
```

## Common Mistakes

### ❌ Wrong: Using 'as' operator

```csharp
var result = _result as Result;  // Compile error
```

### ✅ Correct: Using pattern matching

```csharp
if (_result is Result result) { }
```

### ❌ Wrong: Accessing Detail directly

```csharp
string message = result.Detail;  // Property doesn't exist
```

### ✅ Correct: Accessing through ErrorDetails

```csharp
string message = result.ErrorDetails!.Detail;
```

### ❌ Wrong: Not checking success before accessing Value

```csharp
var tour = result.Value;  // Might throw if IsSuccess is false
```

### ✅ Correct: Check IsSuccess first

```csharp
if (result.IsSuccess)
{
    var tour = result.Value;
}
```

## Error Class Pattern

Create dedicated error classes for each entity:

```csharp
public static class TourErrors
{
    public static Result<Tour> EmptyIdentifier() =>
        Result<Tour>.Failure(
            ResultStatus.Invalid,
            new ResultError("Tour identifier cannot be empty", null));
    
    public static Result EmptyIdentifierForUpdate() =>
        Result.Failure(
            ResultStatus.Invalid,
            new ResultError("Tour identifier cannot be empty", null));
    
    public static Result<Tour> DurationTooShort(int minimum, double actual) =>
        Result<Tour>.Failure(
            ResultStatus.Invalid,
            new ResultError(
                $"Tour duration must be at least {minimum} days, but was {actual} days",
                null));
}
```

## Benefits

1. **Type Safety** - Compiler enforces checking success before accessing value
2. **No Exceptions** - Errors are expected return values, not exceptional cases
3. **Explicit Intent** - Code clearly shows operations can fail
4. **Railway Oriented** - Can chain operations that return Result
5. **Testable** - Easy to test both success and failure paths
