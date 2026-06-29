namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Catalog tour placement for a public media image.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
/// <param name="DisplayOrder">The display order within the tour gallery.</param>
/// <param name="IsCover">Whether the image is the tour cover image.</param>
public sealed record MediaImageTourLink(
    Guid CatalogTourId,
    int DisplayOrder,
    bool IsCover);
