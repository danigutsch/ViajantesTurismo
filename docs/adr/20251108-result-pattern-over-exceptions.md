# ADR-002: Result Pattern Over Exceptions

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Validation failures and business rule violations were throwing exceptions, making error paths implicit and hard to test.

## Decision

Use Result pattern for all domain operations that can fail:

- `Result` for operations with no return value
- `Result<T>` for operations that return a value
- ResultStatus enum for failure categorization
- ResultError record for error details

## Consequences

### Positive

- Explicit error handling at compile time
- No performance overhead of exceptions for expected failures
- Railway-oriented programming enables operation chaining
- Easy to test both success and failure paths

### Negative

- Callers must check IsSuccess before accessing Value
- More verbose than exceptions for truly exceptional cases

## Implementation

```csharp
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public ResultError? ErrorDetails { get; }
}
```

## Links

- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
