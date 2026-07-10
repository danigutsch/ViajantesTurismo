# ADR-003: Validation Constants in Contracts Project

**Status**: Superseded — 2026-07-10

Superseded by the contract split and domain-boundary cleanup. Domain projects now keep domain and
persistence validation limits in domain-owned types such as `AdminDomainLimits` and
`CatalogDomainLimits`; they must not reference `ViajantesTurismo.*.Contracts.*` projects.

## Context

Validation constraints like max lengths, minimum durations, and price limits must be shared consistently across:

- Domain validation logic
- API contract DTOs (for DataAnnotations)
- Test scenarios (Given/When/Then steps)

Duplicating these constants leads to inconsistencies and maintenance burden.

## Original decision

Define all **external validation constraints** in a `ContractConstants` static class within the
Contracts project:

```csharp
public static class ContractConstants
{
    public const int MaxNameLength = 128;
    public const int MinimumTourDurationDays = 5;
    public const double MaxPrice = 100_000;
}
```

API, Web, and test projects may reference these constants for external DTO validation and annotations.
Domain projects do not reference contract projects.

## Consequences

### Pros

- Single source of truth for validation constraints.
- Changes propagate automatically to domain, DTOs, and tests.
- Clear API contract documentation — consumers know the limits.
- No duplication across layers.

### Cons

- Domain no longer references Contracts projects; duplicated domain-specific limit names are preferred
  over an outward dependency from Domain to Contracts.
- Cannot have different constraints for API vs domain (intentional — enforces consistency).

## Alternatives considered

- Constants in domain with duplicates in contracts — rejected due to duplication and drift risk.
- Constants in a shared catch-all project — rejected because constraints are API-contract-specific.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- See `ViajantesTurismo.Admin.Contracts.Application/ContractConstants.cs`
