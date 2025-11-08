# Application layer for mappers and query interfaces

**Status**: Accepted — 2025-11-08

## Context
Mapping logic between domain entities and DTOs was scattered across the API layer. The `IQueryService` interface was mistakenly placed in the Domain layer, but is actually an application concern. No clear place existed for orchestrating multi-aggregate operations.

## Decision
Create a separate **Application layer** (`ViajantesTurismo.Admin.Application`) containing:
- **Mapper classes** for domain ↔ DTO conversions (`BookingMapper`, `CustomerMapper`, `TourMapper`).
- **`IQueryService`** interface for read-only operations returning DTOs.
- **`IUnitOfWork`** interface for transaction management.
- Application services that coordinate multiple domain operations.

## Consequences
**Pros**
- Clear separation between domain logic (business rules) and application orchestration (workflows).
- Mappers isolated from API and domain layers — single responsibility.
- Application services can coordinate multiple aggregates without polluting domain.
- Follows Clean Architecture principles (domain has no knowledge of DTOs).
- Read and write models clearly separated (CQRS-friendly).

**Cons**
- Additional layer adds complexity and more projects to maintain.
- Mapping logic is boilerplate (consider AutoMapper for complex scenarios).

## Alternatives considered
- Keep mappers in API layer — rejected because it couples API to domain entity shapes.
- Use AutoMapper — deferred until mapping complexity justifies it.

## Links
- See `src/ViajantesTurismo.Admin.Application/README.md`
