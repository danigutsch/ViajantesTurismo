using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class PublicMediaImageEntity
{
    private readonly List<PublicMediaImageResponsiveVariantEntity> _responsiveVariants = [];
    private readonly List<PublicMediaImageTourLinkEntity> _tourLinks = [];

    public Guid Id { get; set; }

    public string SourceUri { get; set; } = string.Empty;

    public string Checksum { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public MediaImageProcessingStatus ProcessingStatus { get; set; }

    public string[] Tags { get; set; } = [];

    public string AltText { get; set; } = string.Empty;

    public string? Caption { get; set; }

    public string? Attribution { get; set; }

    public string? Copyright { get; set; }

    public ICollection<PublicMediaImageResponsiveVariantEntity> ResponsiveVariants => _responsiveVariants;

    public ICollection<PublicMediaImageTourLinkEntity> TourLinks => _tourLinks;
}
