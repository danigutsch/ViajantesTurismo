# ADR-015: IQueryable in Query Service for Frontend Optimization

**Date:** 2025-11-15
**Status:** Accepted
**Context:** API design for frontend consumption
**Decision Makers:** Architecture Team

---

## Context

The Admin.Web frontend uses QuickGrid components to display tabular data. The current `IQueryService`
returns materialized collections (`IReadOnlyList<TDto>`), loading all records into memory before sending
to the client.

**Problems:**

- All records loaded regardless of pagination/filtering
- Sorting and filtering execute in-memory instead of database-side
- Performance degrades as datasets grow
- QuickGrid can leverage `IQueryable` for server-side operations but receives materialized data

**QuickGrid with IQueryable:**

```csharp
<QuickGrid Items="@queryService.GetAllBookings()">
    <PropertyColumn Property="b => b.Status" Sortable="true" />
</QuickGrid>
```

Generates optimized SQL: `SELECT * FROM Bookings ORDER BY Status LIMIT 10 OFFSET 0`

With `IReadOnlyList`, all rows are retrieved and sorted in-memory.

## Decision

Change `IQueryService` collection methods to return `IQueryable<TDto>` for deferred execution.

**New Interface:**

```csharp
public interface IQueryService
{
    // Collections - IQueryable for deferred execution
    IQueryable<GetTourDto> GetAllTours();
    IQueryable<GetCustomerDto> GetAllCustomers();
    IQueryable<GetBookingDto> GetAllBookings();

    // Single entities - async with materialization
    Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct);
    Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct);
}
```

**Implementation:**

```csharp
public IQueryable<GetBookingDto> GetAllBookings()
{
    return context.Bookings
        .AsNoTracking()
        .Select(b => new GetBookingDto
        {
            Id = b.Id,
            TourName = b.Tour.Name,
            Status = (BookingStatusDto)b.Status
        });
    // No .ToListAsync() - deferred execution
}
```

## Consequences

**Benefits:**

- Database-side sorting, filtering, pagination
- Constant memory usage regardless of dataset size
- Optimized SQL query generation by EF Core
- QuickGrid automatically leverages deferred execution
- Industry-standard pattern for grid/table APIs

**Drawbacks:**

- Breaking change for existing consumers
- Query executes during enumeration, not method call
- Database errors surface during enumeration
- Different testing patterns required

**Mitigation:**

- Admin.Web is sole consumer—update in lockstep
- ASP.NET Core handles enumeration errors automatically
- Use in-memory database for integration tests

## Alternatives Considered

### 1. Pagination Parameters

Add `page`, `pageSize`, `sortBy` to all collection methods.

*Rejected:* Complex API surface, QuickGrid can't auto-build queries, manual pagination logic required.

### 2. ItemsProvider Pattern

Use Blazor's `GridItemsProvider` with custom pagination endpoints.

*Rejected:* More complex frontend code, doesn't leverage EF Core's IQueryable, less efficient.

### 3. GraphQL

*Rejected:* Over-engineering for single consumer, adds significant complexity.

## Implementation Notes

- Use `.AsNoTracking()` for all collection queries (read-only data)
- Continue projecting to DTOs to avoid exposing domain entities
- Single-entity queries remain async: `Task<TDto?> GetById(Guid id, CancellationToken ct)`

## Related

- **ADR-007**: Application Layer for Mappers and Query Interfaces
- **Integration Test Plan**: API optimization implementation strategy

---

## References

- [ASP.NET Core Blazor QuickGrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid)
- [EF Core Query Performance](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
