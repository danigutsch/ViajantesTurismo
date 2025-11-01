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

## Summary

These architectural decisions establish a robust domain validation approach:

1. **Factory methods** ensure entities are always valid
2. **Result pattern** makes errors explicit and type-safe
3. **Contract constants** provide single source of truth
4. **Error classes** centralize error creation
5. **No comments** keeps code clean and self-documenting
6. **Type-safe testing** handles multiple Result types correctly

This architecture prioritizes:

- Compile-time safety over runtime checks
- Explicit over implicit error handling
- Domain logic centralization
- Testability and maintainability
