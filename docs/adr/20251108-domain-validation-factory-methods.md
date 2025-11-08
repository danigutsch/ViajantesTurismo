# Domain validation with factory methods

**Status**: Accepted — 2025-11-08

## Context
Domain entities were being created with public constructors, allowing invalid state. Validation was scattered across DTOs and application layer, violating DDD principles. Entities could be instantiated in invalid states, and validation logic lacked a single authoritative location.

## Decision
Implement the **factory method pattern** for all aggregate roots:
- Static `Create()` method returns `Result<T>` with all validation.
- Private constructors prevent direct instantiation, ensuring entities are created only through validated factory methods.
- All business rule validation happens in the factory method before construction.
- Update operations return `Result` to indicate success/failure.
- Parameterless private constructor marked `[UsedImplicitly]` for EF Core.

## Consequences
**Pros**
- Entities are always in a valid state — impossible to construct invalid aggregates.
- Validation logic centralized in domain layer, following DDD principles.
- Type-safe error handling with Result pattern.
- Clear separation between domain validation and EF Core requirements.
- Explicit control flow for both creation and updates.

**Cons**
- Additional boilerplate required (factory method + private constructor).
- Callers must check `Result.IsSuccess` before accessing `Value`.

## Alternatives considered
- Public constructors with validation inside — rejected because callers could bypass validation.
- Throwing exceptions for validation failures — rejected in favor of Result pattern (see ADR-002).

## Links
- See `docs/DOMAIN_VALIDATION.md` for validation conventions.
- Related: [ADR-002: Result Pattern Over Exceptions](20251108-result-pattern-over-exceptions.md)
- Related: [ADR-004: Dedicated Error Classes per Entity](20251108-dedicated-error-classes.md)
