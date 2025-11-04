# Architectural Decision Records

## ADR-001: Domain Validation with Factory Methods

**Status:** Implemented

**Context:**
Domain entities were being created with public constructors, allowing invalid state. Validation was scattered across
DTOs and application layer, violating DDD principles.

**Decision:**
Implement factory method pattern for all aggregate roots:

- Static `Create()` method returns `Result<T>`
- Private constructors prevent direct instantiation
- All validation happens in factory method before construction
- Update operations return `Result` to indicate success/failure

**Consequences:**

**Positive:**

- Entities are always in valid state
- Validation logic is centralized in domain
- Type-safe error handling with Result pattern
- Clear separation between valid construction and EF Core needs

**Negative:**

- Additional boilerplate (factory method + private constructor)
- Callers must check Result before accessing Value

**Implementation:**

- `Tour.Create()` validates identifier, name, dates, duration, and prices
- Private constructor only assigns properties
- `[UsedImplicitly]` parameterless constructor for EF Core

---

## ADR-002: Result Pattern Over Exceptions

**Status:** Implemented

**Context:**
Validation failures and business rule violations were throwing exceptions, making error paths implicit and hard to test.

**Decision:**
Use Result pattern for all domain operations that can fail:

- `Result` for operations with no return value
- `Result<T>` for operations that return a value
- ResultStatus enum for failure categorization
- ResultError record for error details

**Consequences:**

**Positive:**

- Explicit error handling at compile time
- No performance overhead of exceptions for expected failures
- Railway-oriented programming enables operation chaining
- Easy to test both success and failure paths

**Negative:**

- Callers must check IsSuccess before accessing Value
- More verbose than exceptions for truly exceptional cases

**Implementation:**

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public ResultError? ErrorDetails { get; }
}
```

---

## ADR-003: Validation Constants in Contracts Project

**Status:** Implemented

**Context:**
Validation constants like max lengths and minimum durations need to be shared between:

- Domain validation logic
- API contract DTOs
- Test scenarios

**Decision:**
Define all external validation constraints in `ContractConstants` class in the Contracts project:

```csharp
public static class ContractConstants
{
    public const int MaxNameLength = 128;
    public const int MinimumTourDurationDays = 5;
    public const double MaxPrice = 100_000;
}
```

**Consequences:**

**Positive:**

- Single source of truth for constraints
- Shared between domain, contracts, and tests
- Changes propagate automatically
- Clear API contract documentation

**Negative:**

- Domain layer references Contracts project
- Cannot have different constraints for API vs domain

**Alternatives Considered:**

- Constants in domain with duplicates in contracts (rejected: duplication)
- Constants in shared Common project (rejected: contracts are API-specific)

---

## ADR-004: Dedicated Error Classes per Entity

**Status:** Implemented

**Context:**
Error creation was duplicated across domain methods, and error messages were inconsistent.

**Decision:**
Create static error classes for each aggregate root:

```csharp
public static class TourErrors
{
    public static Result<Tour> EmptyIdentifier() { }
    public static Result EmptyIdentifierForUpdate() { }
}
```

Provide both generic and non-generic versions:

- `Result<T>` for factory methods
- `Result` for update operations

**Consequences:**

**Positive:**

- Centralized error messages
- Consistent error formatting
- Easy to maintain and update
- Type-safe error creation

**Negative:**

- Requires two versions of each error (generic/non-generic)
- More classes to maintain

---

## ADR-005: No Comments in Domain Code

**Status:** Implemented

**Context:**
Comments in domain code often become outdated and add noise. Well-named methods and validation errors are
self-documenting.

**Decision:**
Remove inline comments from domain entities. Document:

- Public API with XML comments on classes/public methods
- Complex patterns in separate markdown files
- Business rules in error messages themselves

**Consequences:**

**Positive:**

- Code is more readable
- Forces better naming
- No outdated comment maintenance
- Documentation in docs/ folder stays current

**Negative:**

- Less context for complex business rules
- May need to read error classes for validation rules

---

## ADR-006: Type Safety in Test Step Definitions

**Status:** Implemented

**Context:**
Test steps need to handle both `Result` and `Result<T>` return types from different domain operations.

**Decision:**
Use `object?` field with pattern matching in test steps:

```csharp
private object? _result;

if (_result is Result result) { }
else if (_result is Result<Tour> tourResult) { }
```

Never use `as` operator with Result types (they are structs).

**Consequences:**

**Positive:**

- Handles polymorphism correctly
- Compile-time type safety
- Works with struct Result types

**Negative:**

- More verbose than single Result field
- Requires pattern matching in assertions

**Alternatives Considered:**

- Using `as` operator (rejected: doesn't work with structs)
- Separate fields for Result and Result<T> (rejected: verbose)

---

## ADR-007: Application Layer for Mappers and Query Interfaces

**Status:** Implemented

**Context:**
Mapping logic between domain entities and DTOs was scattered across the API layer. The IQueryService interface was in
the Domain layer but is actually an application concern.

**Decision:**
Create a separate Application layer (`ViajantesTurismo.Admin.Application`) containing:

- Mapper classes for domain ↔ DTO conversions
- `IQueryService` interface for read operations
- `IUnitOfWork` interface for transaction management

**Consequences:**

**Positive:**

- Clear separation between domain logic and application orchestration
- Mappers isolated from API and domain layers
- Application services can coordinate multiple domain operations
- Follows Clean Architecture principles

**Negative:**

- Additional layer adds complexity
- More projects to maintain

**Implementation:**

- `BookingMapper`, `CustomerMapper`, `TourMapper` classes
- Static mapping methods (e.g., `MapToBikeType`, `MapToDto`)
- `IQueryService` provides read-only query methods returning DTOs
- Domain layer focuses purely on business logic

---

## ADR-008: TotalPrice as Calculated Property

**Status:** Implemented

**Context:**
Booking total price was stored in the database and could be manually updated, leading to inconsistencies with actual
component prices (base price, room cost, bike prices).

**Decision:**
Make `TotalPrice` a calculated property:

```csharp
public decimal TotalPrice => CalculateTotalPrice(BasePrice, RoomAdditionalCost, PrincipalCustomer, CompanionCustomer);
```

**Consequences:**

**Positive:**

- Price is always correct and consistent
- Single source of truth for pricing logic
- Cannot be manually overridden
- Transparent calculation visible to users

**Negative:**

- Cannot store historical prices if components change
- Migration required to remove TotalPrice column

**Implementation:**

- Removed `TotalPrice` column from database
- Added `entity.Ignore(booking => booking.TotalPrice)` in EF Core configuration
- Formula: `Subtotal - DiscountAmount` where:
    - `Subtotal = BasePrice + RoomAdditionalCost + PrincipalCustomer.BikePrice + CompanionCustomer?.BikePrice ?? 0`
    - `DiscountAmount = Discount.CalculateDiscountAmount(Subtotal)`
- Base price is for single room (not per person)
- Double room adds supplement cost
- Discounts applied after subtotal calculation

---

## ADR-009: Room Pricing Model - Base Price = Single Room

**Status:** Implemented

**Context:**
Initial implementation incorrectly multiplied base price by customer count and added supplement for single rooms.

**Decision:**
Base price represents a single room cost:

- Single room: Base price + 0 supplement
- Double room: Base price + double room supplement
- Companion does NOT multiply base price, only adds bike cost

**Consequences:**

**Positive:**

- Correct business model: double rooms cost MORE than single rooms
- Transparent pricing for customers
- Simpler calculation logic

**Negative:**

- Breaking change requiring data migration
- All existing tests needed updates

**Implementation:**

- Changed `SingleRoomSupplementPrice` → `DoubleRoomSupplementPrice`
- Updated `CalculateRoomAdditionalCost()` to return supplement only for double rooms
- Removed customer count multiplication from price calculation

---

---

## ADR-010: Discount as Value Object with Audit Trail

**Status:** Implemented

**Context:**
Bookings need flexible discount support for promotions, early bird pricing, and custom negotiations. Discounts must be
traceable for audit purposes.

**Decision:**
Implement `Discount` as a value object owned by `Booking`:

```csharp
public sealed class Discount
{
    public DiscountType Type { get; }  // None, Percentage, Absolute
    public decimal Amount { get; }     // 0-100 for percentage, fixed amount for absolute
    public string? Reason { get; }     // Audit trail: "Early bird", "VIP customer", etc.
    
    public decimal CalculateDiscountAmount(decimal subtotal) { }
}
```

**Consequences:**

**Positive:**

- Discount logic encapsulated in single value object
- Reason field provides audit trail
- Type-safe discount types (percentage vs absolute)
- Validation ensures discounts don't exceed subtotal or result in negative price

**Negative:**

- Cannot change discount type without creating new Discount instance
- Reason required when discount applied (adds form complexity)

**Implementation:**

- `DiscountType` enum: None (0), Percentage (1), Absolute (2)
- Percentage validation: 0-100%
- Absolute validation: cannot exceed subtotal
- Final price validation: must be > 0 after discount
- UI shows real-time discount calculation

---

## ADR-011: Payment Tracking with Immutable Payment Records

**Status:** Implemented

**Context:**
Bookings need to track multiple payments over time (down payment, installments, final payment). Payment records must be
immutable for audit and accounting purposes.

**Decision:**
Implement `Payment` as immutable entity with full audit trail:

```csharp
public sealed class Payment : Entity<long>
{
    public long BookingId { get; }
    public decimal Amount { get; }
    public DateTime PaymentDate { get; }
    public PaymentMethod Method { get; }  // CreditCard, BankTransfer, Cash, Check, PayPal, Other
    public string? ReferenceNumber { get; }
    public string? Notes { get; }
    public DateTime RecordedAt { get; }  // Audit: when payment was recorded in system
}
```

Payment status automatically calculated:

- `Unpaid`: No payments recorded
- `PartiallyPaid`: AmountPaid < TotalPrice
- `Paid`: AmountPaid >= TotalPrice

**Consequences:**

**Positive:**

- Complete payment history preserved
- Cannot edit/delete payments (immutability ensures audit integrity)
- Automatic payment status transitions
- Prevents overpayment (validation: amount <= remaining balance)
- Multiple payment methods supported

**Negative:**

- Cannot correct payment errors (must add offsetting payment)
- No refund workflow (would need separate Refund entity)

**Implementation:**

- Payments stored in separate collection: `Booking._payments`
- `AmountPaid` calculated property: `_payments.Sum(p => p.Amount)`
- `RemainingBalance` calculated property: `TotalPrice - AmountPaid`
- `TimeProvider` parameter for testable timestamps
- `RecordedAt` captures when payment entered into system

---

## ADR-012: Booking Details Update After Creation

**Status:** Implemented

**Context:**
Customers may change accommodation or bike preferences after initial booking. Bookings should be modifiable unless in
terminal state (Cancelled/Completed).

**Decision:**
Add `Booking.UpdateDetails()` method allowing post-creation modifications:

```csharp
public Result UpdateDetails(
    RoomType roomType,
    decimal roomAdditionalCost,
    BookingCustomer principalCustomer,
    BookingCustomer? companionCustomer,
    Discount discount)
{
    // Validate not in terminal state
    if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
        return BookingErrors.CannotModifyCancelledOrCompletedBooking(Id, Status);
    
    // Update properties with validation
    // Price recalculates automatically
}
```

**Consequences:**

**Positive:**

- No need to cancel/recreate bookings for simple changes
- Price automatically recalculates with new selections
- Preserves payment history and booking ID
- Customer experience improved (flexible changes)

**Negative:**

- UI more complex (edit forms, confirmation dialogs)
- Price changes may affect payment status
- No audit trail of what changed (would need ADR-009 US-9 audit log)

**Implementation:**

- `PUT /bookings/{id}/details` endpoint
- Can change: room type, bikes, companion, discount
- Cannot change: tour, principal customer, booking date
- Terminal states (Cancelled/Completed) prevent all modifications
- UI shows warning when removing companion from double room

---

## Summary

These architectural decisions establish a robust domain validation approach:

1. **Factory methods** ensure entities are always valid
2. **Result pattern** makes errors explicit and type-safe
3. **Contract constants** provide single source of truth
4. **Error classes** centralize error creation
5. **No comments** keeps code clean and self-documenting
6. **Type-safe testing** handles multiple Result types correctly
7. **Application layer** separates mapping and query concerns from domain
8. **Calculated properties** ensure derived values are always consistent
9. **Correct pricing model** reflects actual business rules
10. **Discount value objects** provide flexible pricing with audit trails
11. **Immutable payments** ensure financial integrity and complete history
12. **Post-creation updates** improve customer experience without compromising data integrity

This architecture prioritizes:

- Compile-time safety over runtime checks
- Explicit over implicit error handling
- Domain logic centralization
- Clean Architecture layering
- Data consistency and correctness
- Testability and maintainability
