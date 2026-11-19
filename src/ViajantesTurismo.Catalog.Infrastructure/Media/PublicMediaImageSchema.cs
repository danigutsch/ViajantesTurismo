namespace ViajantesTurismo.Catalog.Infrastructure.Media;

/// <summary>Database object names for public media image tables.</summary>
internal static class PublicMediaImageSchema
{
    /// <summary>Name of the unique index allowing one cover image per catalog tour.</summary>
    internal const string GalleryCoverUniqueIndex = "UX_PublicMediaImageTourLinks_CatalogTourId_Cover";
    /// <summary>Name of the unique index enforcing distinct display order per catalog tour.</summary>
    internal const string GalleryDisplayOrderUniqueIndex = "UX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder";
    /// <summary>The unique rule of both gallery indexes, reported when a concurrent change took the cover or the display order.</summary>
    internal const string GalleryPlacementRule = "catalog.gallery-placement";
}
