# ADR-003: Validation Constants in Contracts Project

**Status**: Accepted — 2025-11-08

## Context

Validation constraints like max lengths, minimum durations, and price limits must be shared consistently across:

- Domain validation logic
- API contract DTOs (for DataAnnotations)
- Test scenarios (Given/When/Then steps)

Duplicating these constants leads to inconsistencies and maintenance burden.

## Decision

Define all **external validation constraints** in a `ContractConstants` static class within the **Contracts project**:

```csharp
public static class ContractConstants
{
    public const int MaxNameLength = 128;
    public const int MinimumTourDurationDays = 5;
    public const double MaxPrice = 100_000;
}
```

Domain, API, and test projects reference these constants for validation and annotations.

## Consequences

### Pros

- Single source of truth for validation constraints.
- Changes propagate automatically to domain, DTOs, and tests.
- Clear API contract documentation — consumers know the limits.
- No duplication across layers.

### Cons

- Domain layer references Contracts project (acceptable dependency for shared constants).
- Cannot have different constraints for API vs domain (intentional — enforces consistency).

## Alternatives considered

- Constants in domain with duplicates in contracts — rejected due to duplication and drift risk.
- Constants in shared Common project — rejected because constraints are API-contract-specific.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- See `ViajantesTurismo.Admin.Contracts/ContractConstants.cs`
