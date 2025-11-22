# Functional Programming Patterns

This document covers functional programming patterns used throughout ViajantesTurismo projects to handle
operations that can fail (`Result<T>`) and optional values (`Option<T>`).

## Table of Contents

- [Result Pattern](#result-pattern)
- [Option Pattern](#option-pattern)
- [Usage Guidelines](#usage-guidelines)
- [Testing](#testing)

---

## Result Pattern

The Result pattern represents the outcome of an operation that can succeed or fail, replacing exceptions for
expected validation failures.

### Core Types

#### Result (Non-Generic)

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

#### Result\<T\> (Generic)

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

#### ResultError

Represents error information:

```csharp
public record ResultError(
    string Detail,
    Dictionary<string, string[]>? ValidationErrors);
```

#### ResultStatus

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

### Important Notes

#### Result is a Struct

Both `Result` and `Result<T>` are **readonly structs**, not classes:

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

#### Accessing Error Messages

Error details are nested within the `ErrorDetails` property:

```csharp
// ✅ CORRECT
if (!result.IsSuccess)
{
    string message = result.ErrorDetails!.Detail;
}

// ❌ WRONG - Result doesn't have Detail property
string message = result.Detail;
```

### Usage Patterns

#### Domain Operations

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

#### API Endpoints

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

### Error Class Pattern

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

### Benefits

1. **Type Safety** - Compiler enforces checking success before accessing value
2. **No Exceptions** - Errors are expected return values, not exceptional cases
3. **Explicit Intent** - Code clearly shows operations can fail
4. **Railway Oriented** - Can chain operations that return Result
5. **Testable** - Easy to test both success and failure paths

---

## Option Pattern

The Option pattern represents a value that may or may not be present, providing type-safe handling of optional
values without nullable reference types on generic parameters.

### Core Type

```csharp
public readonly struct Option<T> : IEquatable<Option<T>> where T : class
{
    public bool HasValue { get; }
    public T? Value { get; }

    public static Option<T> Of(T value) { }
    public static Option<T> Empty() { }
    public static Option<T> FromNullable(T? value) { }
}
```

### Why Option\<T\>?

**Problem:** Generic Result types with nullable values can be confusing:

```csharp
// ❌ Confusing - Is null a valid success value or an error?
Result<Customer?> FindCustomer(string email);
```

**Solution:** Use `Option<T>` to make intent explicit:

```csharp
// ✅ Clear - Success with no value vs. error are distinct
Result<Option<Customer>> FindCustomer(string email);
```

### Usage Patterns

#### Repository Queries

**Single Entity Lookup:**

```csharp
public async Task<Result<Option<Customer>>> FindByEmailAsync(string email, CancellationToken ct)
{
    try
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Email == email, ct);

        return Result<Option<Customer>>.Success(Option<Customer>.FromNullable(customer));
    }
    catch (Exception ex)
    {
        return Result<Option<Customer>>.Failure(
            ResultStatus.Error,
            new ResultError($"Database error: {ex.Message}", null));
    }
}
```

**Handling the Result:**

```csharp
var result = await _repository.FindByEmailAsync(dto.Email, ct);

if (!result.IsSuccess)
{
    // Database error
    return TypedResults.Problem("Database error occurred");
}

if (!result.Value.HasValue)
{
    // Customer not found
    return TypedResults.NotFound();
}

var customer = result.Value.Value;
return TypedResults.Ok(customer.ToDto());
```

#### Optional Domain Properties

**Companion Customer:**

```csharp
public sealed class Booking
{
    public BookingCustomer PrincipalCustomer { get; private set; }
    public Option<BookingCustomer> CompanionCustomer { get; private set; }

    public static Result<Booking> Create(
        BookingCustomer principal,
        Option<BookingCustomer> companion,
        ...)
    {
        if (companion.HasValue && companion.Value.CustomerId == principal.CustomerId)
        {
            return BookingErrors.CompanionCannotBeSameAsPrincipal();
        }

        return Result<Booking>.Success(new Booking(principal, companion, ...));
    }
}
```

### Factory Methods

**Of - Create with value:**

```csharp
var customer = new Customer(...);
var option = Option<Customer>.Of(customer);
// option.HasValue == true
// option.Value == customer
```

**Empty - Create without value:**

```csharp
var option = Option<Customer>.Empty();
// option.HasValue == false
// option.Value == null
```

**FromNullable - Convert from nullable:**

```csharp
Customer? maybeCustomer = GetCustomerOrNull();
var option = Option<Customer>.FromNullable(maybeCustomer);
// option.HasValue depends on maybeCustomer being null
```

### Important Notes

#### Option is a Struct

Like `Result`, `Option<T>` is a **readonly struct**:

```csharp
// ❌ WRONG - Cannot use 'as' with struct types
var option = _value as Option<Customer>;

// ✅ CORRECT - Use pattern matching
if (_value is Option<Customer> option) { }
```

#### Value Access Safety

The `Value` property uses `MemberNotNullWhen`:

```csharp
if (option.HasValue)
{
    // Compiler knows Value is not null here
    var customer = option.Value;
    Console.WriteLine(customer.Name); // Safe, no null warning
}
```

---

## Usage Guidelines

### When to Use Result\<T\>

Use `Result<T>` for operations that can fail for **expected business reasons**:

- Validation failures
- Business rule violations
- Application-level constraint checks (e.g., uniqueness)
- State transition errors

**Examples:**

```csharp
public static Result<Tour> Create(string identifier, ...); // Validation can fail
public Result UpdateSchedule(DateTime start, DateTime end); // Business rules can fail
public Result<Booking> AddBooking(...); // Capacity or state constraints can fail
```

### When to Use Option\<T\>

Use `Option<T>` for values that **may or may not exist** but are not errors:

- Optional domain entities (companion customer, optional notes)
- Query results that may return nothing (find by email)
- Optional configuration values

**Examples:**

```csharp
public Option<BookingCustomer> CompanionCustomer { get; } // May not have companion
public Task<Result<Option<Customer>>> FindByEmailAsync(string email); // May not exist
```

### When to Use Nullable\<T\> (T?)

Use nullable types for **simple optional primitives or value types**:

```csharp
public string? Notes { get; } // Optional string
public int? Age { get; } // Optional age
public DateTime? CompletedAt { get; } // Optional completion date
```

### Combining Result and Option

**Query that might fail OR return nothing:**

```csharp
// Repository method
public async Task<Result<Option<Customer>>> FindByIdAsync(Guid id, CancellationToken ct)
{
    try
    {
        var customer = await _dbContext.Customers.FindAsync([id], ct);
        return Result<Option<Customer>>.Success(Option<Customer>.FromNullable(customer));
    }
    catch (Exception ex)
    {
        return Result<Option<Customer>>.Failure(
            ResultStatus.Error,
            new ResultError($"Database error: {ex.Message}", null));
    }
}

// Handler usage
var result = await _repository.FindByIdAsync(command.CustomerId, ct);

if (!result.IsSuccess)
    return result; // Database error

if (!result.Value.HasValue)
    return CustomerErrors.NotFound(command.CustomerId); // Not found

var customer = result.Value.Value;
// Work with customer
```

---

## Testing

### Testing Result\<T\>

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

### Testing Option\<T\>

**With Value:**

```csharp
[Then(@"the companion should be present")]
public void ThenCompanionShouldBePresent()
{
    var booking = _result as Booking;
    Assert.NotNull(booking);
    Assert.True(booking.CompanionCustomer.HasValue);
    Assert.Equal(expectedCompanionId, booking.CompanionCustomer.Value.CustomerId);
}
```

**Without Value:**

```csharp
[Then(@"the companion should not be present")]
public void ThenCompanionShouldNotBePresent()
{
    var booking = _result as Booking;
    Assert.NotNull(booking);
    Assert.False(booking.CompanionCustomer.HasValue);
}
```

### Testing Combined Result\<Option\<T\>\>

```csharp
[Then(@"the customer should not be found")]
public void ThenCustomerShouldNotBeFound()
{
    Assert.NotNull(_result);

    if (_result is Result<Option<Customer>> result)
    {
        Assert.True(result.IsSuccess); // Query succeeded
        Assert.False(result.Value.HasValue); // But returned no customer
    }
    else
    {
        Assert.Fail("Expected Result<Option<Customer>>");
    }
}
```

---

## Common Mistakes

### ❌ Wrong: Using 'as' operator with structs

```csharp
var result = _result as Result;  // Compile error
var option = _value as Option<Customer>;  // Compile error
```

### ✅ Correct: Using pattern matching

```csharp
if (_result is Result result) { }
if (_value is Option<Customer> option) { }
```

### ❌ Wrong: Accessing Value without checking

```csharp
var tour = result.Value;  // Might throw if IsSuccess is false
var customer = option.Value;  // Might be null if HasValue is false
```

### ✅ Correct: Check first

```csharp
if (result.IsSuccess)
{
    var tour = result.Value;
}

if (option.HasValue)
{
    var customer = option.Value;
}
```

### ❌ Wrong: Using Option for errors

```csharp
// Don't use Option to represent "not found" errors
public Option<Customer> FindCustomer(string email); // No way to distinguish error from not found
```

### ✅ Correct: Use Result\<Option\<T\>\>

```csharp
// Use Result<Option<T>> to distinguish database errors from "not found"
public Task<Result<Option<Customer>>> FindCustomerAsync(string email, CancellationToken ct);
```

---

## Related Documentation

- **[Domain Validation](../../docs/DOMAIN_VALIDATION.md)** — Factory methods and validation patterns
- **[Coding Guidelines](../../docs/CODING_GUIDELINES.md)** — Error handling and when to throw exceptions
- **[Test Guidelines](../../docs/TEST_GUIDELINES.md)** — Testing patterns for Result and Option
