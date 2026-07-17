using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents an image in a public Catalog tour gallery.
/// </summary>
public sealed record CatalogTourImageDto
{
    /// <summary>
    /// Gets the stable media image identifier.
    /// </summary>
    [Required]
    public Guid Id { get; init; }

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
    /// Gets the accessible image description.
    /// </summary>
    [StringLength(ContractConstants.MaxAltTextLength)]
    public required string AltText { get; init; }

    /// <summary>
    /// Gets a value indicating whether this image is intentionally decorative.
    /// </summary>
    [Required]
    public bool IsDecorative { get; init; }

    /// <summary>
    /// Gets an optional display caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption { get; init; }

    /// <summary>
    /// Gets processed rendition metadata for responsive image rendering.
    /// </summary>
    [Required, MinLength(1)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The contract uses source-generated JSON serialization for responsive variant metadata.")]
    public IReadOnlyList<CatalogMediaImageVariantDto> ResponsiveVariants { get; init; } = [];
}
