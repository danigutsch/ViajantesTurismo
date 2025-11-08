# ADR-001: Domain Validation with Factory Methods

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Domain entities were being created with public constructors, allowing invalid state. Validation was scattered across
DTOs and application layer, violating DDD principles.

## Decision

Implement factory method pattern for all aggregate roots:

- Static `Create()` method returns `Result<T>`
- Private constructors prevent direct instantiation
- All validation happens in factory method before construction
- Update operations return `Result` to indicate success/failure

## Consequences

### Positive

- Entities are always in valid state
- Validation logic is centralized in domain
- Type-safe error handling with Result pattern
- Clear separation between valid construction and EF Core needs

### Negative

- Additional boilerplate (factory method + private constructor)
- Callers must check Result before accessing Value

## Implementation

- `Tour.Create()` validates identifier, name, dates, duration, and prices
- Private constructor only assigns properties
- `[UsedImplicitly]` parameterless constructor for EF Core

## Links

- Related: [ADR-002: Result Pattern Over Exceptions](20251108-result-pattern-over-exceptions.md)
- Related: [ADR-004: Dedicated Error Classes per Entity](20251108-dedicated-error-classes.md)
