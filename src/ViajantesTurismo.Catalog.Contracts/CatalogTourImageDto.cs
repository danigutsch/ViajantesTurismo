using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Represents an image in a public Catalog tour gallery.
/// </summary>
public sealed record CatalogTourImageDto
{
    /// <summary>
    /// Gets the display order for the image inside its tour gallery.
    /// </summary>
    [Required, Range(0, int.MaxValue)]
    public int SortOrder { get; init; }

    /// <summary>
    /// Gets a value indicating whether this image is the preferred tour cover.
    /// </summary>
    [Required]
    public bool IsCover { get; init; }

    /// <summary>
    /// Gets the public image URI.
    /// </summary>
    [Required]
    public required Uri Uri { get; init; }

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxAltTextLength, MinimumLength = 1)]
    public required string AltText { get; init; }

    /// <summary>
    /// Gets an optional display caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption { get; init; }

    /// <summary>
    /// Gets processed variants for responsive image rendering.
    /// </summary>
    [Required]
    public IReadOnlyList<MediaImageResponsiveVariantDto> ResponsiveVariants { get; init; } = [];
}
