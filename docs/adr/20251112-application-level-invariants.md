# ADR-014: Application-level invariants for uniqueness constraints

**Status**: Accepted (Completed) — 2025-11-12; completed on 2025-11-13

## Context

Some business invariants cannot be enforced at the aggregate level because they require
checking data outside the aggregate boundary. Examples include:

- **INV-CUST-001**: Email addresses must be unique across all customers
- **INV-TOUR-001**: Tour identifiers must be unique across all tours

These invariants require querying the persistence store to verify uniqueness, which
violates DDD principles if done within the aggregate.

## Decision

Implement a two-layer validation approach for uniqueness constraints:

1. **Domain Layer (Aggregate)**:
    - Enforces all invariants that can be validated using only aggregate state
    - Does NOT perform database queries or call repositories
    - Remains pure domain logic with no infrastructure dependencies

2. **Application Layer (Command Handlers/Services)**:
    - Enforces uniqueness invariants by querying repositories before calling aggregate methods
    - Uses repository interfaces (e.g., `ICustomerStore.EmailExists()`, `ITourStore.IdentifierExists()`)
    - Returns validation errors using the same Result pattern as domain validation
    - Maintains consistency by checking uniqueness within the same transaction

### Implementation Pattern

#### Application-level Invariants (Uniqueness)

**For Creation (new entities):**

```csharp
// Infrastructure Layer - Repository Interface
public interface ICustomerStore
{
    Task<bool> EmailExists(string email, CancellationToken ct);
}

// Infrastructure Layer - EF Core Implementation
public async Task<bool> EmailExists(string email, CancellationToken ct)
{
    return await dbContext.Customers
        .AnyAsync(c => c.ContactInfo.Email == email, ct);
}

// Application Layer - Command Handler
public async Task<Result<Customer>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
{
    // Application-level invariant validation
    if (await customerStore.EmailExists(cmd.Email, ct))
    {
        return Result<Customer>.Invalid(
            detail: $"A customer with email '{cmd.Email}' already exists.",
            field: "email",
            message: "Email address already exists.");
    }

    // Domain-level invariant validation
    var result = Customer.Create(/* ... */);
    if (result.IsFailure)
    {
        return result;
    }

    // Persist
    await customerStore.Add(result.Value, ct);
    return result;
}
```

**For Updates (existing entities):**

```csharp
// Infrastructure Layer - Repository Interface
public interface ITourStore
{
    Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct);
}

// Infrastructure Layer - EF Core Implementation
public async Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct)
{
    return await dbContext.Tours
        .AnyAsync(t => t.Identifier == identifier && t.Id != excludeTourId, ct);
}

// Application Layer - Update Command Handler
public async Task<Result> Handle(UpdateTourCommand cmd, CancellationToken ct)
{
    var tour = await tourStore.GetById(cmd.TourId, ct);
    if (tour is null)
    {
        return TourErrors.TourNotFound(cmd.TourId).ConvertError();
    }

    // Application-level invariant validation (excluding current entity)
    if (await tourStore.IdentifierExistsExcluding(cmd.Identifier, cmd.TourId, ct))
    {
        return TourErrors.IdentifierAlreadyExists(cmd.Identifier);
    }

    // Domain-level validations
    var detailsResult = tour.UpdateDetails(cmd.Identifier, cmd.Name);
    if (detailsResult.IsFailure)
    {
        return detailsResult;
    }

    // ... other domain updates ...

    await unitOfWork.SaveEntities(ct);
    return Result.Ok();
}
```

#### Domain-level Invariants (Business Rules in Handlers)

When domain invariants require orchestration (get entity, validate, persist), implement in handlers:

```csharp
// Domain Layer - Aggregate Method
public sealed class Tour
{
    public Result Delete()
    {
        var confirmedBookingsCount = Bookings.Count(b => b.Status == BookingStatus.Confirmed);
        if (confirmedBookingsCount > 0)
        {
            return TourErrors.CannotDeleteTourWithConfirmedBookings(confirmedBookingsCount);
        }

        // Domain event can be raised here for later
        return Result.Ok();
    }
}

// Application Layer - Command Handler
public async Task<Result> Handle(DeleteTourCommand cmd, CancellationToken ct)
{
    var tour = await tourStore.GetById(cmd.TourId, ct);
    if (tour is null)
    {
        return TourErrors.TourNotFound(cmd.TourId).ConvertError();
    }

    // Domain-level business rule validation
    var deleteResult = tour.Delete();
    if (deleteResult.IsFailure)
    {
        return deleteResult;
    }

    // Persist
    tourStore.Delete(tour);
    await unitOfWork.SaveEntities(ct);
    return Result.Ok();
}
```

### Behavior Test Pattern

For BDD tests, create fake in-memory stores that use collections:

```csharp
// Test Context - Shared state for behavior tests
public sealed class CustomerContext
{
    public ICollection<Customer> Customers { get; } = [];
}

// Step Definitions - Validate against in-memory collection
[When(@"I attempt to create another customer with email ""(.*)""")]
public void WhenIAttemptToCreateAnotherCustomerWithEmail(string email)
{
    var emailExists = context.Customers.Any(c => c.ContactInfo.Email == email);
    if (emailExists)
    {
        bookingContext.Result = Result.Invalid(
            detail: $"A customer with email '{email}' already exists.",
            field: "email",
            message: "Email address already exists.");
        return;
    }

    // Create customer normally...
}
```

## Consequences

### Pros

- **Preserves aggregate purity**: Aggregates remain free of infrastructure dependencies
- **Maintains DDD principles**: Clear separation between domain and application concerns
- **Consistent error handling**: Same Result pattern for all validation errors
- **Testable**: Fake stores enable in-memory testing without database
- **Transaction safety**: Uniqueness checks happen within same transaction as persistence
- **Performance**: Single efficient query rather than loading all entities

### Cons

- **Two validation layers**: Developers must remember which invariants go where
- **Race conditions**: Small window between check and insert (mitigated by database unique constraints)
- **Duplicate validation logic**: BDD tests must replicate uniqueness checks in step definitions

### Mitigation Strategies

1. **Database constraints**: Always add unique indexes/constraints as final safeguard
2. **Clear documentation**: Document application-level invariants in feature files with `@Invariant:INV-XXX-NNN` tags
3. **Naming convention**: Repository methods named `XExists()` clearly indicate uniqueness checks
4. **Testing**: Both unit tests (aggregate) and integration tests (handler + repository)

## Alternatives considered

### Option 1: Check uniqueness in aggregate Create() method

**Rejected** because:

- Violates DDD: Aggregates should not depend on repositories
- Breaks domain layer purity
- Makes aggregates harder to test
- Creates circular dependency issues

### Option 2: Use domain services

**Rejected** because:

- Domain services still in domain layer, shouldn't query infrastructure
- Application layer is the natural boundary for orchestration
- Command handlers already coordinate between domain and infrastructure

### Option 3: Event-based eventual consistency

**Rejected** for uniqueness constraints because:

- Business requires immediate validation (cannot accept duplicate emails)
- Added complexity not justified for simple uniqueness checks
- Race conditions more problematic with async validation

## TODO: Implementation Tasks

We will refactor ALL endpoints that perform business logic (i.e., invoke aggregates, mutate state, enforce invariants,
or coordinate persistence) to delegate that logic to application command handlers. Query-only endpoints (pure reads)
may remain thin for now but can later be migrated to query handlers / mediators as part of cross-cutting concerns
(logging, metrics, tracing, auth, resilience).

- [x] **Refactor all mutation endpoints to use Command Handlers (scoped to invariants for this ADR)**
    - [x] `CustomerEndpoints.CreateCustomer` → `CreateCustomerCommandHandler` (INV-CUST-001)
    - [x] `CustomerEndpoints.UpdateCustomer` → `UpdateCustomerCommandHandler`
    (email uniqueness via `EmailExistsExcluding`) (INV-CUST-001)
        - [x] `TourEndpoints.CreateTour` → `CreateTourCommandHandler` (INV-TOUR-001)
        - [x] `TourEndpoints.UpdateTour` → `UpdateTourCommandHandler` (INV-TOUR-001 - identifier uniqueness on update)
        - [x] `TourEndpoints.DeleteTour` → `DeleteTourCommandHandler` (INV-TOUR-015)

  The broader Booking endpoint refactors and other general handlerization tasks have been moved to `USER_STORIES.md`.

- [x] **Add/Enhance Unit Tests for Handlers**
    - [x] Create fake stores for testing handlers in isolation (FakeCustomerStore, FakeTourStore)
    - [x] Test application-level invariant validation
      (email uniqueness via INV-CUST-001, identifier uniqueness via INV-TOUR-001)
    - [x] Test domain-level invariant validation in handlers (tour deletion with confirmed bookings via INV-TOUR-015)
    - [x] Add unit tests for `UpdateCustomerCommandHandler` (success path, not-found, email uniqueness conflict)

General cross-cutting, integration test strategy, and documentation tasks have been moved to `USER_STORIES.md`.

## Completion

All ADR-scoped implementation tasks are complete and verified by the full automated test suite as of 2025-11-13.
Out-of-scope items (e.g., broader booking handlerization and cross-cutting pipeline) are tracked in `USER_STORIES.md`.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- See `docs/DOMAIN_VALIDATION.md` for validation patterns
- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
- Related: [ADR-002: Result Pattern Over Exceptions](20251108-result-pattern-over-exceptions.md)
- Examples: `INV-CUST-001` in `CustomerCreation.feature`, `INV-TOUR-001` in `TourCreation.feature`
