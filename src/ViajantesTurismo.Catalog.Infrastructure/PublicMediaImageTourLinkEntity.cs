namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class PublicMediaImageTourLinkEntity
{
    public Guid PublicMediaImageId { get; set; }

    public Guid CatalogTourId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCover { get; set; }
}
