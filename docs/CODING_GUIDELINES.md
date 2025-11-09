# Domain Validation & Modeling Guidelines

Domain entities enforce their own invariants through factory methods and update operations, ensuring entities are never
in an invalid state. Use these conventions to keep the domain model consistent and testable.

## Ubiquitous Language

Maintain consistent terminology across code, API contracts, documentation, and Gherkin feature files. All domain terms
should have the same meaning everywhere they appear.

**Reference:** See `docs/domain/GLOSSARY.md` for the canonical domain vocabulary.

## Aggregates & Invariants

For each Aggregate, document:

- **Purpose**: What business capability it protects
- **Invariants**: Rules that must always hold (enforced atomically within the aggregate boundary)
- **Commands**: State-changing operations the aggregate handles
- **Events**: Domain events emitted on state changes
- **Policies**: Cross-aggregate/domain rules implemented as domain services or process managers

**Reference:** See [ADR-001: Domain Validation with Factory Methods](adr/20251108-domain-validation-factory-methods.md)
and aggregate documentation.

### Current Aggregates

#### Tour Aggregate

- **Purpose**: Manages tour offerings and their bookings
- **Root**: `Tour` entity
- **Invariants**:
    - Tour dates must span minimum duration
    - Prices must be non-negative
    - Bookings cannot exceed maximum capacity
    - Confirmed bookings count toward capacity
- **Entities**: `Tour`, `Booking`, `Payment`

#### Customer Aggregate

- **Purpose**: Manages customer information and profiles
- **Root**: `Customer` entity
- **Invariants**:
    - Email must be unique and valid format
    - Customer must be 18+ years old
    - Contact information properly formatted

## Result Pattern

All domain operations that can fail return `Result` or `Result<T>`.

**Reference:** See [Result Pattern documentation](../src/ViajantesTurismo.Common/RESULT_PATTERN.md)
and [ADR-002: Result Pattern Over Exceptions](adr/20251108-result-pattern-over-exceptions.md).

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

## Value Objects

Prefer whole values over primitives when they have domain meaning and business rules:

- **Money**: Encapsulates amount and currency with proper equality
- **DateRange**: Validates end date > start date
- **Discount**: Encapsulates discount type, amount, and reason with validation
- **BookingCustomer**: Groups customer ID, bike type, and bike price

**Principles:**

- Immutable with factory methods returning `Result<T>`
- Validation encapsulated inside the value object
- Correct structural equality (override `Equals`/`GetHashCode` or use records)

**Example:**

```csharp
public sealed record Money(decimal Amount, Currency Currency)
{
    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            return Result<Money>.Invalid("Amount cannot be negative");
        
        return new Money(amount, currency);
    }
}
```

**Reference:** See [ADR-010: Discount as Value Object](adr/20251108-discount-value-object.md)

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

**Reference:**
See [ADR-003: Validation Constants in Contracts Project](adr/20251108-validation-constants-contracts-project.md)

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
## Domain Events

Domain events communicate state changes within and across bounded contexts:

### Domain Events (Synchronous)

In-process events handled synchronously within the same transaction:

**Naming Convention:**
- Past tense: `BookingConfirmed`, `CustomerUpgraded`, `PaymentRecorded`
- Suffix: `DomainEvent` (e.g., `BookingConfirmedDomainEvent`)

**Characteristics:**
- Handled synchronously within the same transaction
- Used for domain invariants that span aggregates
- Failures roll back the entire transaction
- Minimal data needed by domain handlers

**Example:**
```csharp
public sealed record BookingConfirmedDomainEvent(long BookingId, int TourId, DateTime ConfirmedAt);
```

### Integration Events (Asynchronous)

Cross-boundary events published to external systems or bounded contexts:

**Naming Convention:**

- Past tense: `BookingConfirmed`, `CustomerUpgraded`
- Suffix: `IntegrationEvent` (e.g., `BookingConfirmedIntegrationEvent`)

**Characteristics:**

- Published via outbox pattern for reliability
- Consumed asynchronously by subscribers
- Can include denormalized data for consumers
- No immediate consistency guarantee

**Example:**

```csharp
public sealed record BookingConfirmedIntegrationEvent(
    long BookingId, 
    int TourId, 
    string TourName,
    decimal TotalPrice,
    DateTime ConfirmedAt);

## Gherkin Scenarios as Living Documentation

Business-facing scenarios live under `tests/specs` and serve as executable documentation:

**Principles:**
- Use `Rule:` blocks to group scenarios by invariant
- Mirror aggregate terms in Given/When/Then steps
- Prefer declarative steps over imperative
- Tag scenarios with aggregate and ADR references

**Example:**
```gherkin
@Agg:Tour @ADR:20251108-domain-validation-factory-methods
Feature: Tour Creation

  Rule: Tour identifier must be unique and non-empty
    
    Scenario: Create tour with valid identifier
      Given a tour identifier "CUBA2024"
      When I create a tour with that identifier
      Then the tour should be created successfully
```

**Reference:** See [TEST_GUIDELINES.md](TEST_GUIDELINES.md) for BDD conventions.

## Testing

**Reference:** See [ADR-006: Type Safety in Test Step Definitions](adr/20251108-type-safety-test-step-definitions.md)

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

## Related Documentation

- [Architectural Decision Records](ARCHITECTURE_DECISIONS.md) — Core architectural patterns and decisions
- [Coding Guidelines](CODING_GUIDELINES.md) — C# coding standards and conventions
- [Test Guidelines](TEST_GUIDELINES.md) — Testing patterns and BDD scenarios
- [Result Pattern](../src/ViajantesTurismo.Common/RESULT_PATTERN.md) — Detailed Result\<T\> usage

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

## Testing Validation Errors Validation Errors

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
