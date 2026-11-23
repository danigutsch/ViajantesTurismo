namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>The next image awaiting an accessibility review for a Catalog tour.</summary>
/// <param name="Image">The pending image, or <see langword="null"/> when no image requires a review.</param>
public sealed record CatalogPendingImageReviewDto(CatalogMediaImageDto? Image);
