# ADR-003: Validation Constants in Contracts Project

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Validation constants like max lengths and minimum durations need to be shared between:

- Domain validation logic
- API contract DTOs
- Test scenarios

## Decision

Define all external validation constraints in `ContractConstants` class in the Contracts project:

```csharp
public static class ContractConstants
{
    public const int MaxNameLength = 128;
    public const int MinimumTourDurationDays = 5;
    public const double MaxPrice = 100_000;
}
```

## Consequences

### Positive

- Single source of truth for constraints
- Shared between domain, contracts, and tests
- Changes propagate automatically
- Clear API contract documentation

### Negative

- Domain layer references Contracts project
- Cannot have different constraints for API vs domain

## Alternatives

- **Constants in domain with duplicates in contracts** — Rejected due to duplication
- **Constants in shared Common project** — Rejected because contracts are API-specific
