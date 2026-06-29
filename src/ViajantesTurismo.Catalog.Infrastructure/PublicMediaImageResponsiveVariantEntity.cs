namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class PublicMediaImageResponsiveVariantEntity
{
    public Guid PublicMediaImageId { get; set; }

    public int SortOrder { get; set; }

    public string Uri { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }
}
