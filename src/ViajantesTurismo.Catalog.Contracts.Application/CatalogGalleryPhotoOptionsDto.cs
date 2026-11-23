namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>A counted page of management gallery photo choices.</summary>
/// <param name="Items">The bounded set of choices.</param>
/// <param name="Total">The total number of matching photos.</param>
public sealed record CatalogGalleryPhotoOptionsDto(IReadOnlyList<CatalogGalleryPhotoOptionDto> Items, int Total);
