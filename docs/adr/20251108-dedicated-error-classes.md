# ADR-004: Dedicated Error Classes per Entity

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Error creation was duplicated across domain methods, and error messages were inconsistent.

## Decision

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

## Consequences

### Positive

- Centralized error messages
- Consistent error formatting
- Easy to maintain and update
- Type-safe error creation

### Negative

- Requires two versions of each error (generic/non-generic)
- More classes to maintain

## Links

- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
- Related: [ADR-002: Result Pattern Over Exceptions](20251108-result-pattern-over-exceptions.md)
