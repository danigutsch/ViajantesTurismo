using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SharedKernel.BuildingBlocks;
using SharedKernel.InputNormalization;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Localized accessibility text for a public media image.
/// </summary>
public sealed class PublicMediaImageAccessibilityText : ValueObject
{
    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    private PublicMediaImageAccessibilityText()
    {
    }

    private PublicMediaImageAccessibilityText(
        PublicContentLanguage language,
        string? altText,
        string? caption,
        bool isDecorative,
        bool requiresHumanReview,
        bool isAiGenerated)
    {
        Language = language;
        AltText = altText;
        Caption = caption;
        IsDecorative = isDecorative;
        RequiresHumanReview = requiresHumanReview;
        IsAiGenerated = isAiGenerated;
    }

    /// <summary>
    /// Gets the language for this accessibility text.
    /// </summary>
    public PublicContentLanguage Language { get; private set; }

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    public string? AltText { get; private set; }

    /// <summary>
    /// Gets the optional public caption.
    /// </summary>
    public string? Caption { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the image is intentionally decorative.
    /// </summary>
    public bool IsDecorative { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a human must review this text before publication.
    /// </summary>
    public bool RequiresHumanReview { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current text was generated as an AI-assisted draft.
    /// </summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this text can be published.
    /// </summary>
    public bool IsReviewedForPublication => !RequiresHumanReview && (IsDecorative || !string.IsNullOrWhiteSpace(AltText));

    /// <summary>
    /// Creates an AI-assisted draft that requires human review.
    /// </summary>
    /// <param name="language">The text language.</param>
    /// <param name="altText">The drafted accessible image description.</param>
    /// <param name="caption">The optional drafted caption.</param>
    /// <returns>A result containing the accessibility text when valid.</returns>
    public static Result<PublicMediaImageAccessibilityText> CreateAiDraft(
        PublicContentLanguage language,
        string altText,
        string? caption)
    {
        return Create(language, altText, caption, isDecorative: false, requiresHumanReview: true, isAiGenerated: true);
    }

    /// <summary>
    /// Creates reviewed accessibility text from editor-approved content.
    /// </summary>
    /// <param name="language">The text language.</param>
    /// <param name="altText">The approved accessible image description.</param>
    /// <param name="caption">The optional approved caption.</param>
    /// <param name="isDecorative">Whether the image is intentionally decorative.</param>
    /// <returns>A result containing the accessibility text when valid.</returns>
    public static Result<PublicMediaImageAccessibilityText> CreateReviewed(
        PublicContentLanguage language,
        string? altText,
        string? caption,
        bool isDecorative)
    {
        return Create(language, altText, caption, isDecorative, requiresHumanReview: false, isAiGenerated: false);
    }

    /// <summary>
    /// Creates accessibility text with explicit review state.
    /// </summary>
    /// <param name="language">The text language.</param>
    /// <param name="altText">The accessible image description.</param>
    /// <param name="caption">The optional caption.</param>
    /// <param name="isDecorative">Whether the image is intentionally decorative.</param>
    /// <param name="requiresHumanReview">Whether a human must review this text before publication.</param>
    /// <param name="isAiGenerated">Whether the current text was generated as an AI-assisted draft.</param>
    /// <returns>A result containing the accessibility text when valid.</returns>
    public static Result<PublicMediaImageAccessibilityText> Create(
        PublicContentLanguage language,
        string? altText,
        string? caption,
        bool isDecorative,
        bool requiresHumanReview,
        bool isAiGenerated)
    {
        var sanitizedAltText = StringSanitizer.Sanitize(altText);
        var sanitizedCaption = StringSanitizer.Sanitize(caption);
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (language == PublicContentLanguage.None || !Enum.IsDefined(language))
        {
            errors[nameof(Language)] = ["Language is required."];
        }

        if (!requiresHumanReview && !isDecorative && string.IsNullOrWhiteSpace(sanitizedAltText))
        {
            errors[nameof(AltText)] = ["Alt text is required unless the image is decorative."];
        }

        if (isDecorative && !string.IsNullOrWhiteSpace(sanitizedAltText))
        {
            errors[nameof(IsDecorative)] = ["Decorative images must not publish alt text."];
        }

        if (isAiGenerated && !requiresHumanReview)
        {
            errors[nameof(RequiresHumanReview)] = ["AI-generated accessibility text requires human review."];
        }

        if (sanitizedAltText?.Length > ContractConstants.MaxAltTextLength)
        {
            errors[nameof(AltText)] = [$"Alt text cannot exceed {ContractConstants.MaxAltTextLength} characters."];
        }

        if (sanitizedCaption?.Length > ContractConstants.MaxCaptionLength)
        {
            errors[nameof(Caption)] = [$"Caption cannot exceed {ContractConstants.MaxCaptionLength} characters."];
        }

        return errors.Count > 0
            ? Result.Invalid<PublicMediaImageAccessibilityText>("Public media accessibility text is invalid.", errors)
            : Result.Ok(new PublicMediaImageAccessibilityText(language, sanitizedAltText, sanitizedCaption, isDecorative, requiresHumanReview, isAiGenerated));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Language;
        yield return AltText;
        yield return Caption;
        yield return IsDecorative;
        yield return RequiresHumanReview;
        yield return IsAiGenerated;
    }
}
