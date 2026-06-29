namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Public metadata for a media image used by Catalog tours.
/// </summary>
/// <param name="Id">The stable media image identifier.</param>
/// <param name="SourceUri">The original source URI.</param>
/// <param name="Checksum">The content checksum supplied by the media source.</param>
/// <param name="ContentType">The source media content type.</param>
/// <param name="FileSizeBytes">The source file size in bytes.</param>
/// <param name="Dimensions">The source image dimensions.</param>
/// <param name="ProcessingStatus">The external processing state.</param>
/// <param name="ResponsiveVariants">The public responsive renditions.</param>
/// <param name="Tags">The editorial tags for discovery and grouping.</param>
/// <param name="TourLinks">The tour gallery placements.</param>
/// <param name="AltText">The accessible image description.</param>
/// <param name="Caption">The optional public caption.</param>
/// <param name="Attribution">The optional attribution text.</param>
/// <param name="Copyright">The optional copyright notice.</param>
public sealed record PublicMediaImage(
    Guid Id,
    Uri SourceUri,
    string Checksum,
    string ContentType,
    long FileSizeBytes,
    MediaImageDimensions Dimensions,
    MediaImageProcessingStatus ProcessingStatus,
    IReadOnlyList<MediaImageResponsiveVariant> ResponsiveVariants,
    IReadOnlyList<string> Tags,
    IReadOnlyList<MediaImageTourLink> TourLinks,
    string AltText,
    string? Caption,
    string? Attribution,
    string? Copyright);
