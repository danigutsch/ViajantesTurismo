# ADR-015: Query Service Return Types - IQueryable vs IReadOnlyList

**Date:** 2025-11-15
**Status:** Rejected (Both IQueryable and Server-Side Pagination)
**Context:** API design for frontend data presentation
**Decision Makers:** Architecture Team

---

## Context

The Admin.Web frontend uses QuickGrid components to display tabular data. We need to decide what type
the `IQueryService` should return for collection queries.

## Options Considered

1. **IQueryable\<TDto>** - Deferred execution, client controls filtering/sorting
2. **Server-side pagination with PagedRequest/PagedResult** - Explicit pagination DTOs
3. **Simple IReadOnlyList\<TDto>** - Materialized collections (current approach)

### Initial Consideration: IQueryable

Considered returning `IQueryable<TDto>` for deferred execution with QuickGrid. However, this approach has significant
drawbacks:

- **Leaky abstraction** - Exposes EF Core implementation details to API consumers
- **Limited extensibility** - Doesn't support other presentation frameworks well
- **Testing complexity** - Requires in-memory database or complex mocking
- **Security risks** - Difficult to control what queries clients can build
- **Tight coupling** - Ties API to specific ORM behavior

### Second Consideration: Server-Side Pagination

Also considered explicit pagination DTOs with `PagedRequest` and `PagedResult<T>`. While this approach
has benefits for large datasets, it was deemed unnecessary complexity for the current scale:

- Current datasets are small (tens to hundreds of records, not thousands)
- Client-side pagination with QuickGrid works well for this scale
- Additional complexity not justified by current requirements
- Can be added later if/when dataset sizes require it

## Decision

**Keep the current simple approach: return `IReadOnlyList<TDto>` from query services.**

### Service Interface (Current Approach)

```csharp
public interface IBookingQueryService
{
    Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct);
    Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct);
}
```

### Implementation Example

```csharp
public async Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct)
{
    return await context.Bookings
        .AsNoTracking()
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new GetBookingDto
        {
            Id = b.Id,
            TourId = b.TourId,
            TourName = b.Tour.Name,
            CustomerId = b.PrincipalCustomerId,
            CustomerName = $"{b.PrincipalCustomer.FirstName} {b.PrincipalCustomer.LastName}",
            Status = (BookingStatusDto)b.Status,
            TotalPrice = b.TotalPrice,
            CreatedAt = b.CreatedAt
        })
        .ToListAsync(ct);
}
```

### QuickGrid Integration

```razor
@page "/bookings"
@inject IBookingQueryService BookingService

<QuickGrid Items="@bookings" Pagination="pagination">
    <PropertyColumn Property="b => b.CustomerName" Sortable="true" />
    <PropertyColumn Property="b => b.TourName" Sortable="true" />
    <PropertyColumn Property="b => b.Status" Sortable="true" />
    <PropertyColumn Property="b => b.TotalPrice" Sortable="true" Format="C" />
    <PropertyColumn Property="b => b.CreatedAt" Sortable="true" Format="yyyy-MM-dd" />
</QuickGrid>

<Paginator State="pagination" />

@code {
    private IQueryable<GetBookingDto>? bookings;
    private PaginatorState pagination = new() { ItemsPerPage = 25 };

    protected override async Task OnInitializedAsync()
    {
        var allBookings = await BookingService.GetAllBookings(CancellationToken.None);
        bookings = allBookings.AsQueryable();
    }
}
```

## Consequences

### Benefits

✅ **Simplicity** - Straightforward API with minimal complexity
✅ **Easy to understand** - Clear, simple contracts
✅ **Easy to test** - Simple unit tests with no mocking complexity
✅ **QuickGrid native binding** - Direct `Items` binding with client-side sorting/filtering
✅ **Sufficient for current scale** - Works well for current dataset sizes (tens to hundreds of records)
✅ **No over-engineering** - Avoids unnecessary complexity

### Drawbacks

❌ **Loads all records** - Not suitable for large datasets (thousands of records)
❌ **Client-side operations** - Sorting/filtering happen in browser memory
❌ **Network overhead** - All data transferred even if not displayed
❌ **Future refactoring** - May need server-side pagination later if datasets grow

### When to Revisit

Consider implementing server-side pagination if:

- Any single collection exceeds ~1000 records
- Page load times become noticeable (>2 seconds)
- Network transfer sizes become problematic
- Users request advanced filtering capabilities

## Alternatives Considered

### 1. IQueryable\<TDto> (Rejected)

Return `IQueryable<TDto>` from query services for deferred execution.

**Rejected because:**

- Leaky abstraction exposing EF Core details
- Poor testability without in-memory database
- Security concerns with uncontrolled query building
- Tight coupling to specific ORM
- Not suitable for API boundaries (only works in-process)

### 2. Server-Side Pagination with PagedRequest/PagedResult (Rejected)

Use explicit pagination DTOs with query parameters.

**Rejected because:**

- Over-engineering for current dataset sizes
- Current datasets are small (tens to hundreds, not thousands)
- Client-side pagination with QuickGrid works well at this scale
- Additional complexity not justified by current requirements
- Can be added later if needed (YAGNI principle)

### 3. OData Protocol (Rejected)

Use OData conventions for querying (`$filter`, `$orderby`, `$top`, `$skip`).

**Rejected because:**

- Massive over-engineering for single internal consumer
- Parsing complexity for filter expressions
- Security risks with unvalidated filter strings
- Additional dependencies

### 4. GraphQL (Rejected)

Client-controlled query schema.

**Rejected because:**

- Significant complexity for single consumer
- Additional dependencies and learning curve
- Over-engineering for internal admin panel

## Implementation Notes

1. **Query services** return `IReadOnlyList<TDto>` for all collection queries

2. **Use AsNoTracking()** for all read-only queries

3. **QuickGrid binding** uses direct `Items` binding with `.AsQueryable()` for client-side operations

4. **Default sorting** applied in query services (typically by CreatedAt descending)

5. **Keep datasets small** - monitor collection sizes and revisit if growth exceeds reasonable limits

## Related

- **ADR-007**: Application Layer for Mappers and Query Interfaces
- **Integration Test Plan**: API optimization implementation strategy

---

## References

- [ASP.NET Core Blazor QuickGrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid)
- [EF Core Query Performance](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [REST API Pagination Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#filter-and-paginate-data)
