# ADR-006: Type Safety in Test Step Definitions

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Test steps need to handle both `Result` and `Result<T>` return types from different domain operations.

## Decision

Use `object?` field with pattern matching in test steps:

```csharp
private object? _result;

if (_result is Result result) { }
else if (_result is Result<Tour> tourResult) { }
```

Never use `as` operator with Result types (they are structs).

## Consequences

### Positive

- Handles polymorphism correctly
- Compile-time type safety
- Works with struct Result types

### Negative

- More verbose than single Result field
- Requires pattern matching in assertions

## Alternatives

- **Using `as` operator** — Rejected because it doesn't work with structs
- **Separate fields for Result and Result<T>** — Rejected due to verbosity
