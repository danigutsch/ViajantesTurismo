# Dedicated error classes per aggregate root

**Status**: Accepted — 2025-11-08

## Context

Error creation was duplicated across domain methods, and error messages were inconsistent. Each validation failure constructed `Result` inline, leading to scattered error messages and duplication.

## Decision

Create **static error classes** for each aggregate root (e.g., `TourErrors`, `BookingErrors`, `CustomerErrors`):

```csharp
public static class TourErrors
{
    public static Result<Tour> EmptyIdentifier() => Result.Failure<Tour>(
        ResultStatus.Invalid, "Tour identifier cannot be empty.");
    
    public static Result EmptyIdentifierForUpdate() => Result.Failure(
        ResultStatus.Invalid, "Tour identifier cannot be empty.");
}
```

Provide both:

- `Result<T>` for factory methods (creation operations).
- `Result` for update operations (no returned entity).

## Consequences

### Pros

- Centralized error messages — single location per aggregate.
- Consistent error formatting and status codes.
- Easy to maintain, update, and localize error messages.
- Type-safe error creation with clear naming.
- Improved testability — errors are reusable across tests.

### Cons

- Requires two versions of each error (generic/non-generic) — minor duplication.
- More classes to maintain (one per aggregate).

## Alternatives considered

- Inline error construction in domain methods — rejected due to duplication and inconsistency.
- Single global `Errors` class — rejected as it doesn't scale with many aggregates.

## Links

- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
- Related: [ADR-002: Result Pattern Over Exceptions](20251108-result-pattern-over-exceptions.md)
