# Type safety in test step definitions with pattern matching

**Status**: Accepted — 2025-11-08

## Context

Behavior test steps need to handle both `Result` (no return value) and `Result<T>` (typed return value) from different domain operations. `Result` and `Result<T>` are value types (structs), not classes, so standard polymorphism techniques don't apply.

## Decision

Use an `object?` field with **pattern matching** in test step definitions:

```csharp
private object? _result;

// In Then steps:
if (_result is Result result) 
{
    result.IsSuccess.Should().BeFalse();
}
else if (_result is Result<Tour> tourResult) 
{
    tourResult.IsSuccess.Should().BeTrue();
}
```

**Never use the `as` operator** with Result types — it doesn't work with structs and always returns null.

## Consequences

### Pros

- Handles Result polymorphism correctly with compile-time type safety.
- Works with struct-based Result types (value semantics).
- Pattern matching is explicit and type-safe.

### Cons

- More verbose than a single `Result` field.
- Requires pattern matching in every assertion step.

## Alternatives considered

- Using `as` operator — rejected because it doesn't work with structs (always null).
- Separate fields for `Result` and `Result<T>` — rejected due to excessive verbosity and duplication.
- Making Result a class — rejected to preserve value semantics and performance.

## Links

- See `tests/ViajantesTurismo.Admin.BehaviorTests/StepDefinitions/*.cs`
