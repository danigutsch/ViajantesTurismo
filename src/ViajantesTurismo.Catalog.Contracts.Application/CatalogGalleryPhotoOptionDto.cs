namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>A gallery photo choice and its optional management preview rendition.</summary>
/// <param name="Id">The Catalog photo ID.</param>
/// <param name="Label">The short name used when displaying the choice.</param>
/// <param name="PreviewWidth">The rendition width, when a preview is available.</param>
/// <param name="PreviewContentType">The rendition media type, when a preview is available.</param>
public sealed record CatalogGalleryPhotoOptionDto(Guid Id, string Label, int? PreviewWidth, string? PreviewContentType);
