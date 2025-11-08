# ADR-007: Application Layer for Mappers and Query Interfaces

**Date:** 2025-11-08  
**Status:** Accepted

## Context

Mapping logic between domain entities and DTOs was scattered across the API layer. The IQueryService interface was in
the Domain layer but is actually an application concern.

## Decision

Create a separate Application layer (`ViajantesTurismo.Admin.Application`) containing:

- Mapper classes for domain ↔ DTO conversions
- `IQueryService` interface for read operations
- `IUnitOfWork` interface for transaction management

## Consequences

### Positive

- Clear separation between domain logic and application orchestration
- Mappers isolated from API and domain layers
- Application services can coordinate multiple domain operations
- Follows Clean Architecture principles

### Negative

- Additional layer adds complexity
- More projects to maintain

## Implementation

- `BookingMapper`, `CustomerMapper`, `TourMapper` classes
- Static mapping methods (e.g., `MapToBikeType`, `MapToDto`)
- `IQueryService` provides read-only query methods returning DTOs
- Domain layer focuses purely on business logic
