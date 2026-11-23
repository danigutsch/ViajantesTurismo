namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>A window of entries in the Management content overview.</summary>
/// <param name="Items">The overview entries in canonical key order.</param>
/// <param name="Total">The overall number of editable content entries.</param>
public sealed record PublicContentOverviewPageDto(IReadOnlyList<PublicContentOverviewItemDto> Items, int Total)
{
    /// <summary>The largest number of entries in an overview page.</summary>
    public const int ItemsPerPage = 20;
}
