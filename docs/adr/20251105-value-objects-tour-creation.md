# ADR-013: Value Objects for Primitive Parameter Grouping

**Date:** 2025-11-05  
**Status:** Superseded

## Context

Factory methods with many primitive parameters (strings, decimals, ints, DateTimes) become difficult to read, maintain,
and use correctly. Related parameters are not grouped logically, leading to:

- Long parameter lists (10+ parameters)
- Easy to mix up parameter order
- Difficult to discover which parameters are related
- Validation logic scattered across factory method
- Adding new related parameters requires signature changes

## Decision

This decision was superseded by keeping primitive parameters in factory methods.

The original approach introduced value objects to group related parameters, but was reverted in favor of:

- Keeping factory methods with primitive parameters for simplicity
- Direct validation in the factory method
- No intermediate value object construction required

## Consequences

### Original Value Object Approach (Reverted)

**Positive:**

- Reduced parameter count through logical grouping
- Validation encapsulated in value objects
- Reusable components across domain
- Better IntelliSense discoverability
- Easier to extend related parameters

**Negative:**

- More types to understand and maintain
- Callers must construct value objects first
- More verbose for simple cases
- Additional layer of abstraction

### Current Primitive Approach

**Positive:**

- Simpler for callers (no value object construction)
- Direct parameter validation
- Less abstraction overhead

**Negative:**

- Long parameter lists
- Related parameters not grouped
- Validation scattered in factory method

## Links

- **Superseded by:** Current implementation using primitive parameters in factory methods
