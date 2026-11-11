using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents editor-approved accessibility text for a media image.
/// </summary>
public sealed record PublicMediaImageAccessibilityReviewRequest
{
    /// <summary>
    /// Gets the language tag of the accessibility text.
    /// </summary>
    [Required]
    public string Language { get; init; } = PublicContentLanguageTags.EnglishUnitedStates;

    /// <summary>
    /// Gets the approved alternative text.
    /// </summary>
    [StringLength(ContractConstants.MaxAltTextLength)]
    public string? AltText
    {
        get; init;
    }

    /// <summary>
    /// Gets the optional approved caption.
    /// </summary>
    [StringLength(ContractConstants.MaxCaptionLength)]
    public string? Caption
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether the image is intentionally decorative.
    /// </summary>
    [Required]
    public bool IsDecorative
    {
        get; init;
    }
}
