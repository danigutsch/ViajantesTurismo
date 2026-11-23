namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>One bounded page of the Management tour list.</summary>
/// <param name="Items">Tour summaries in the requested order.</param>
/// <param name="Total">The total number of tours before paging.</param>
public sealed record CatalogTourPageDto(IReadOnlyList<CatalogTourListItemDto> Items, int Total)
{
    /// <summary>The fixed maximum number of tours per Management list page.</summary>
    public const int ItemsPerPage = 10;
}
