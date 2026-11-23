namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Describes a page of financial records or action choices.</summary>
/// <param name="Section">The section being paged.</param>
/// <param name="ParentId">The owning record for a nested section.</param>
/// <param name="Page">The one-based page returned.</param>
/// <param name="PageSize">The maximum number of items in the window.</param>
/// <param name="TotalCount">The total number of items in the section.</param>
/// <param name="PageCount">The total number of pages in the section.</param>
public sealed record BookingFinancialPageWindowDto(BookingFinancialSection Section, Guid? ParentId, int Page, int PageSize, int TotalCount, int PageCount);
