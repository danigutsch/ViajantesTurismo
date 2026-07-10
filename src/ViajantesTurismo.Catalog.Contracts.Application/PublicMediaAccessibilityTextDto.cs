using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Localized accessibility text for a public media image.
/// </summary>
public sealed record PublicMediaAccessibilityTextDto
{
    /// <summary>
    /// Gets the language for this accessibility text.
    /// </summary>
    [Required]
    public PublicContentLanguageDto Language { get; init; }

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    [StringLength(ContractConstants.MaxAltTextLength)]
    public string? AltText { get; init; }

    /// <summary>
    /// Gets the optional public caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption { get; init; }

    /// <summary>
    /// Gets a value indicating whether the image is intentionally decorative for this language.
    /// </summary>
    [Required]
    public bool IsDecorative { get; init; }

    /// <summary>
    /// Gets a value indicating whether a human must review this text before publication.
    /// </summary>
    [Required]
    public bool RequiresHumanReview { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current text was generated as an AI-assisted draft.
    /// </summary>
    [Required]
    public bool IsAiGenerated { get; init; }
}
