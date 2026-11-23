namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>One page of published Catalog albums.</summary>
/// <param name="Page">The current page number, starting at one.</param>
/// <param name="PageSize">The maximum number of albums per page.</param>
/// <param name="Total">The number of eligible albums across the gallery.</param>
/// <param name="Albums">The album summaries in display order.</param>
public sealed record CatalogGalleryIndexDto(int Page, int PageSize, int Total, IReadOnlyList<CatalogGallerySummaryDto> Albums);
