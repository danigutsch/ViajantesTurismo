# ADR-002: Result Pattern Over Exceptions

**Status**: Accepted — 2025-11-08

## Context

Validation failures and business rule violations were throwing exceptions, making error paths implicit
and hard to test. Exceptions for expected domain failures introduce performance overhead and obscure
control flow.

## Decision

Use the **Result pattern** for all domain operations that can fail:

- `Result` for operations with no return value.
- `Result<T>` for operations that return a value on success.
- `ResultStatus` enum categorizes failures (Failure, Invalid, NotFound, Conflict, etc.).
- `ResultError` record carries error details (status, message, context).
- Reserve exceptions for truly exceptional, unrecoverable failures (e.g., infrastructure errors).

## Consequences

### Pros

- Explicit error handling at compile time — failure paths are visible in method signatures.
- No performance overhead of exceptions for expected validation/business failures.
- Railway-oriented programming enables operation chaining (`Map`, `Bind`).
- Easy to test both success and failure paths with deterministic outcomes.
- Clear separation between domain errors and infrastructure exceptions.

### Cons

- Callers must check `IsSuccess` before accessing `Value`.
- More verbose than exceptions for truly exceptional cases.
- Requires team discipline to use consistently.

## Alternatives considered

- Throwing exceptions for validation failures — rejected due to performance and implicit control flow.
- Using nullable types for failure — rejected as it doesn't carry error information.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
