# Value objects for primitive parameter grouping

**Status**: Superseded — 2025-11-05

## Context

Factory methods with many primitive parameters (strings, decimals, ints, DateTimes) become difficult to read, maintain, and use correctly. Related parameters are not grouped logically, leading to:

- Long parameter lists (10+ parameters)
- Easy to mix up parameter order
- Difficult to discover which parameters are related
- Validation logic scattered across factory method
- Adding new related parameters requires signature changes

## Decision (Superseded)

This decision was **superseded** by keeping primitive parameters in factory methods.

The original approach introduced value objects to group related parameters (e.g., `PricingInfo`, `ScheduleInfo`), but was reverted in favor of:

- Keeping factory methods with **primitive parameters** for simplicity.
- Direct validation in the factory method.
- No intermediate value object construction required.

## Consequences

### Original Value Object Approach (Reverted)

*Pros:*

- Reduced parameter count through logical grouping.
- Validation encapsulated in value objects.
- Reusable components across domain.
- Better IntelliSense discoverability.
- Easier to extend related parameters.

*Cons:*

- More types to understand and maintain.
- Callers must construct value objects first.
- More verbose for simple cases.
- Additional layer of abstraction.

### Current Primitive Approach

*Pros:*

- Simpler for callers (no intermediate value object construction).
- Direct parameter validation in factory method.
- Less abstraction overhead.

*Cons:*

- Long parameter lists (reduced readability).
- Related parameters not grouped.
- Validation concentrated in factory method.

## Alternatives considered

- Builder pattern for complex factory methods — deferred until complexity justifies it.
- Parameter objects (DTOs) — rejected as they don't provide validation encapsulation.

## Links

- **Superseded by:** Current implementation using primitive parameters in factory methods (no ADR documenting reversal).
