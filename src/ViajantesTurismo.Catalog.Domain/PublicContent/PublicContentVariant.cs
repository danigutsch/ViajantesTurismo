using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Localized editable public website content for one supported language.
/// </summary>
public sealed class PublicContentVariant : ValueObject
{
    private const int MaxBodyLength = 4000;

    private PublicContentVariant(
        PublicContentLanguage language,
        string title,
        string body,
        string? seoTitle,
        string? metaDescription,
        string? shareSummary,
        bool requiresHumanReview)
    {
        Language = language;
        Title = title;
        Body = body;
        SeoTitle = seoTitle;
        MetaDescription = metaDescription;
        ShareSummary = shareSummary;
        RequiresHumanReview = requiresHumanReview;
    }

    /// <summary>
    /// Gets the content language.
    /// </summary>
    public PublicContentLanguage Language { get; }

    /// <summary>
    /// Gets the public page title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the main public page body text.
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Gets the SEO title when it differs from the page title.
    /// </summary>
    public string? SeoTitle { get; }

    /// <summary>
    /// Gets the SEO meta description.
    /// </summary>
    public string? MetaDescription { get; }

    /// <summary>
    /// Gets the social sharing summary.
    /// </summary>
    public string? ShareSummary { get; }

    /// <summary>
    /// Gets a value indicating whether a human must review this variant before publication.
    /// </summary>
    public bool RequiresHumanReview { get; }

    /// <summary>
    /// Creates localized content with validation.
    /// </summary>
    /// <param name="language">The content language.</param>
    /// <param name="title">The page title.</param>
    /// <param name="body">The page body.</param>
    /// <param name="seoTitle">The optional SEO title.</param>
    /// <param name="metaDescription">The optional SEO meta description.</param>
    /// <param name="shareSummary">The optional social sharing summary.</param>
    /// <param name="requiresHumanReview">Whether this variant needs human review.</param>
    /// <returns>A result containing the content variant when valid.</returns>
    public static Result<PublicContentVariant> Create(
        PublicContentLanguage language,
        string title,
        string body,
        string? seoTitle,
        string? metaDescription,
        string? shareSummary,
        bool requiresHumanReview)
    {
        var sanitizedTitle = StringSanitizer.Sanitize(title);
        var sanitizedBody = StringSanitizer.SanitizeNotes(body);
        var sanitizedSeoTitle = StringSanitizer.Sanitize(seoTitle);
        var sanitizedMetaDescription = StringSanitizer.Sanitize(metaDescription);
        var sanitizedShareSummary = StringSanitizer.Sanitize(shareSummary);
        var errors = new ValidationErrors();

        ValidateSupportedLanguage(errors, language);
        ValidateRequiredText(errors, nameof(Title), sanitizedTitle, ContractConstants.MaxNameLength);
        ValidateRequiredText(errors, nameof(Body), sanitizedBody, MaxBodyLength);
        ValidateOptionalText(errors, nameof(SeoTitle), sanitizedSeoTitle, ContractConstants.MaxNameLength);
        ValidateOptionalText(errors, nameof(MetaDescription), sanitizedMetaDescription, ContractConstants.MaxCaptionLength);
        ValidateOptionalText(errors, nameof(ShareSummary), sanitizedShareSummary, ContractConstants.MaxCaptionLength);

        if (errors.HasErrors)
        {
            return errors.ToResult<PublicContentVariant>();
        }

        return Result.Ok(new PublicContentVariant(
            language,
            sanitizedTitle,
            sanitizedBody,
            sanitizedSeoTitle,
            sanitizedMetaDescription,
            sanitizedShareSummary,
            requiresHumanReview));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Language;
        yield return Title;
        yield return Body;
        yield return SeoTitle;
        yield return MetaDescription;
        yield return ShareSummary;
        yield return RequiresHumanReview;
    }

    private static void ValidateSupportedLanguage(ValidationErrors errors, PublicContentLanguage language)
    {
        if (language is not PublicContentLanguage.EnUs and not PublicContentLanguage.PtBr)
        {
            errors.Add(PublicContentErrors.UnsupportedLanguage(nameof(Language)));
        }
    }

    private static void ValidateRequiredText(
        ValidationErrors errors,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(PublicContentErrors.EmptyText(field));
        }
        else if (value.Length > maxLength)
        {
            errors.Add(PublicContentErrors.TextTooLong(field, maxLength, value.Length));
        }
    }

    private static void ValidateOptionalText(
        ValidationErrors errors,
        string field,
        string? value,
        int maxLength)
    {
        if (value?.Length > maxLength)
        {
            errors.Add(PublicContentErrors.TextTooLong(field, maxLength, value.Length));
        }
    }
}
