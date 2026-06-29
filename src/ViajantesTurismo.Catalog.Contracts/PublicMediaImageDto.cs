using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Public metadata for a media image used by Catalog tour galleries.
/// </summary>
public sealed record PublicMediaImageDto
{
    /// <summary>
    /// Gets the stable media image identifier.
    /// </summary>
    [Required]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the original source URI.
    /// </summary>
    [Required]
    public required Uri SourceUri { get; init; }

    /// <summary>
    /// Gets the content checksum supplied by the media source.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxChecksumLength, MinimumLength = 1)]
    public required string Checksum { get; init; }

    /// <summary>
    /// Gets the source media content type.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxContentTypeLength, MinimumLength = 1)]
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the source file size in bytes.
    /// </summary>
    [Required, Range(0, long.MaxValue)]
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the source image dimensions.
    /// </summary>
    [Required]
    public required MediaImageDimensionsDto Dimensions { get; init; }

    /// <summary>
    /// Gets the external processing state.
    /// </summary>
    [Required]
    public required MediaImageProcessingStatusDto ProcessingStatus { get; init; }

    /// <summary>
    /// Gets the public responsive renditions.
    /// </summary>
    [Required]
    public required IReadOnlyList<MediaImageResponsiveVariantDto> ResponsiveVariants { get; init; }

    /// <summary>
    /// Gets the editorial tags for discovery and grouping.
    /// </summary>
    [Required]
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Gets the tour gallery placements.
    /// </summary>
    [Required]
    public required IReadOnlyList<MediaImageTourLinkDto> TourLinks { get; init; }

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxAltTextLength, MinimumLength = 1)]
    public required string AltText { get; init; }

    /// <summary>
    /// Gets the optional public caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption { get; init; }

    /// <summary>
    /// Gets the optional attribution text.
    /// </summary>
    [StringLength(ContractConstants.MaxAttributionLength)]
    public string? Attribution { get; init; }

    /// <summary>
    /// Gets the optional copyright notice.
    /// </summary>
    [StringLength(ContractConstants.MaxCopyrightLength)]
    public string? Copyright { get; init; }
}
