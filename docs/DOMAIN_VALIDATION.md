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

## Application vs Domain Validation

Not all business rules belong inside aggregates. Use the right layer for the right rule:

- Domain invariants:
    - Pure rules that depend only on the aggregate’s own state
    - Enforced in factory methods and update operations
    - Examples: value ranges, state transitions, composition validity

- Application-level invariants:
    - Rules requiring persistence or cross-aggregate checks (e.g., uniqueness)
    - Enforced in application command handlers/services using repositories
    - Return Result/Result\<T> and are mapped to HTTP ProblemDetails in the API

See ADR [2025-11-12 Application-level uniqueness invariants](adr/20251112-application-level-invariants.md)
for the rationale and patterns.

**Reference:** See [domain/AGGREGATES.md](domain/AGGREGATES.md) for detailed aggregate invariants and business rules.

## Result Pattern

All domain operations that can fail return `Result` or `Result<T>`.

**Reference:** See [Functional Programming Patterns](../src/ViajantesTurismo.Common/FUNCTIONAL_PATTERNS.md)
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

Validation rules are enforced through factory methods and update operations. Each aggregate maintains
its own invariants.

**For specific validation rules and constraints, see:**

- [Aggregates Documentation](domain/AGGREGATES.md) - Detailed invariants for Tour, Customer, and Booking
- [Glossary](domain/GLOSSARY.md) - Domain terminology and enum definitions
- [Contract Constants](../src/ViajantesTurismo.Admin.Contracts/ContractConstants.cs) - Shared validation constants

**Example validation in factory method:**

```csharp
public static Result<Tour> Create(string identifier, string name, ...)
{
    if (string.IsNullOrWhiteSpace(identifier))
        return TourErrors.EmptyIdentifier();

    if (identifier.Length > ContractConstants.MaxIdentifierLength)
        return TourErrors.IdentifierTooLong();

    // Additional validations...

    return new Tour(identifier, name, ...);
}
```

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

## Application-level Uniqueness Patterns

Enforce uniqueness constraints in the application layer using repositories. Typical cases:

- INV-CUST-001: Customer email must be unique
- INV-TOUR-001: Tour identifier must be unique

Repository methods:

```csharp
public interface ICustomerStore
{
    Task<bool> EmailExists(string email, CancellationToken ct);
    Task<bool> EmailExistsExcluding(string email, Guid excludeCustomerId, CancellationToken ct);
}

public interface ITourStore
{
    Task<bool> IdentifierExists(string identifier, CancellationToken ct);
}
```

Handler example (Update Customer):

```csharp
public async Task<Result> Handle(UpdateCustomerCommand command, CancellationToken ct)
{
    var customer = await _store.FindByIdAsync(command.CustomerId, ct);
    if (customer is null) return Result.NotFound($"Customer {command.CustomerId} not found");

    if (await _store.EmailExistsExcluding(command.ContactInfo.Email, command.CustomerId, ct))
        return Result.Invalid(
            detail: $"A customer with email '{command.ContactInfo.Email}' already exists.",
            field: nameof(command.ContactInfo.Email),
            message: "Email must be unique");

    var update = customer.Update(
        /* map DTOs to value objects here */);
    if (!update.IsSuccess) return update;

    await _unitOfWork.SaveEntities(ct);
    return Result.Ok();
}
```

Testing strategy:

- Unit tests for handlers using fake repositories (in-memory collections)
- Behavior tests cover specification scenarios with fakes
- Integration tests ensure API returns RFC 7807 ValidationProblem responses

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

### Tour Creation

```csharp
var result = Tour.Create(dto.Identifier, dto.Name, /* value objects */);
if (!result.IsSuccess)
    return result.ToValidationProblem();

var tour = result.Value;
await unitOfWork.SaveEntities(ct);
```

### Recording a Payment

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

## Related Documentation

- [Architectural Decision Records](ARCHITECTURE_DECISIONS.md) — Core architectural patterns and decisions
- [Coding Guidelines](CODING_GUIDELINES.md) — C# coding standards and conventions
- [Test Guidelines](TEST_GUIDELINES.md) — Testing patterns and BDD scenarios
- [Functional Programming Patterns](../src/ViajantesTurismo.Common/FUNCTIONAL_PATTERNS.md) — Result\<T>
  and Option\<T> usage
- [Aggregates](domain/AGGREGATES.md) — Business invariants and domain operations
- [Glossary](domain/GLOSSARY.md) — Domain terminology and concepts
