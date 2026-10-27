namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Database object names for public media image tables.</summary>
internal static class PublicMediaImageSchema
{
    /// <summary>Name of the unique index allowing one cover image per catalog tour.</summary>
    internal const string GalleryCoverUniqueIndex = "UX_PublicMediaImageTourLinks_CatalogTourId_Cover";
    /// <summary>Name of the unique index enforcing distinct display order per catalog tour.</summary>
    internal const string GalleryDisplayOrderUniqueIndex = "UX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder";
}
