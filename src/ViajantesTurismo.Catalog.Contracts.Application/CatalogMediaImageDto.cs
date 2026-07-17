using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents a Catalog media image for authenticated management workflows.
/// </summary>
public sealed record CatalogMediaImageDto
{
    /// <summary>
    /// Gets the stable media image identifier.
    /// </summary>
    [Required]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets responsive preview rendition metadata.
    /// </summary>
    [Required]
    public required IReadOnlyList<CatalogMediaImageVariantDto> ResponsiveVariants { get; init; }

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    [StringLength(ContractConstants.MaxAltTextLength)]
    public required string AltText { get; init; }

    /// <summary>
    /// Gets the optional display caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption { get; init; }

    /// <summary>
    /// Gets a value indicating whether the image is intentionally decorative.
    /// </summary>
    [Required]
    public bool IsDecorative { get; init; }

    /// <summary>
    /// Gets a value indicating whether accessibility text needs human review.
    /// </summary>
    [Required]
    public bool RequiresHumanReview { get; init; }

    /// <summary>
    /// Gets a value indicating whether the accessibility text is AI-assisted.
    /// </summary>
    [Required]
    public bool IsAiGenerated { get; init; }
}
