using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Business-editable public website content with English and Brazilian Portuguese variants.
/// </summary>
public sealed class EditablePublicContent : AggregateRoot<Guid>
{
    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    private EditablePublicContent()
    {
        Key = string.Empty;
        EnUs = null!;
        PtBr = null!;
    }

    private EditablePublicContent(
        Guid id,
        string key,
        PublicContentLanguage sourceLanguage,
        PublicContentVariant enUs,
        PublicContentVariant ptBr,
        PublicContentPublicationState publicationState)
        : base(id)
    {
        Key = key;
        SourceLanguage = sourceLanguage;
        EnUs = enUs;
        PtBr = ptBr;
        PublicationState = publicationState;
    }

    /// <summary>
    /// Gets the stable content key, such as a page or section identifier.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Gets the language originally entered by the editor.
    /// </summary>
    public PublicContentLanguage SourceLanguage { get; private set; }

    /// <summary>
    /// Gets the English content variant.
    /// </summary>
    public PublicContentVariant EnUs { get; private set; }

    /// <summary>
    /// Gets the Brazilian Portuguese content variant.
    /// </summary>
    public PublicContentVariant PtBr { get; private set; }

    /// <summary>
    /// Gets the publication state for the content entry.
    /// </summary>
    public PublicContentPublicationState PublicationState { get; private set; }

    /// <summary>
    /// Creates editable public website content with both supported language variants.
    /// </summary>
    /// <param name="key">The stable content key.</param>
    /// <param name="sourceLanguage">The source language entered by the editor.</param>
    /// <param name="enUs">The English variant.</param>
    /// <param name="ptBr">The Brazilian Portuguese variant.</param>
    /// <returns>A result containing editable content when valid.</returns>
    public static Result<EditablePublicContent> Create(
        string key,
        PublicContentLanguage sourceLanguage,
        PublicContentVariant enUs,
        PublicContentVariant ptBr)
    {
        ArgumentNullException.ThrowIfNull(enUs);
        ArgumentNullException.ThrowIfNull(ptBr);

        var sanitizedKey = StringSanitizer.Sanitize(key);
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(sanitizedKey))
        {
            errors.Add(PublicContentErrors.EmptyKey());
        }
        else if (sanitizedKey.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(PublicContentErrors.KeyTooLong(ContractConstants.MaxDefaultLength, sanitizedKey.Length));
        }

        ValidateSupportedSourceLanguage(errors, sourceLanguage);
        ValidateVariantLanguage(errors, nameof(EnUs), enUs, PublicContentLanguage.EnUs);
        ValidateVariantLanguage(errors, nameof(PtBr), ptBr, PublicContentLanguage.PtBr);

        return errors.HasErrors
            ? errors.ToResult<EditablePublicContent>()
            : Result.Ok(new EditablePublicContent(
                Guid.CreateVersion7(),
                sanitizedKey,
                sourceLanguage,
                enUs,
                ptBr,
                GetInitialPublicationState(enUs, ptBr)));
    }

    /// <summary>
    /// Publishes content after all language variants have passed human review.
    /// </summary>
    /// <returns>A result indicating whether publication was allowed.</returns>
    public Result Publish()
    {
        if (EnUs.RequiresHumanReview || PtBr.RequiresHumanReview)
        {
            return PublicContentErrors.ReviewRequiredBeforePublishing();
        }

        PublicationState = PublicContentPublicationState.Published;
        return Result.Ok();
    }

    /// <summary>
    /// Replaces the editable language variants for the same content key.
    /// </summary>
    /// <param name="sourceLanguage">The source language entered by the editor.</param>
    /// <param name="enUs">The English variant.</param>
    /// <param name="ptBr">The Brazilian Portuguese variant.</param>
    /// <returns>A result indicating whether replacement was allowed.</returns>
    public Result ReplaceVariants(
        PublicContentLanguage sourceLanguage,
        PublicContentVariant enUs,
        PublicContentVariant ptBr)
    {
        ArgumentNullException.ThrowIfNull(enUs);
        ArgumentNullException.ThrowIfNull(ptBr);

        var errors = new ValidationErrors();

        ValidateSupportedSourceLanguage(errors, sourceLanguage);
        ValidateVariantLanguage(errors, nameof(EnUs), enUs, PublicContentLanguage.EnUs);
        ValidateVariantLanguage(errors, nameof(PtBr), ptBr, PublicContentLanguage.PtBr);

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        SourceLanguage = sourceLanguage;
        EnUs = enUs;
        PtBr = ptBr;
        PublicationState = GetInitialPublicationState(enUs, ptBr);

        return Result.Ok();
    }

    private static PublicContentPublicationState GetInitialPublicationState(
        PublicContentVariant enUs,
        PublicContentVariant ptBr)
    {
        return enUs.RequiresHumanReview || ptBr.RequiresHumanReview
            ? PublicContentPublicationState.ReviewRequired
            : PublicContentPublicationState.Draft;
    }

    private static void ValidateSupportedSourceLanguage(
        ValidationErrors errors,
        PublicContentLanguage sourceLanguage)
    {
        if (sourceLanguage is not PublicContentLanguage.EnUs and not PublicContentLanguage.PtBr)
        {
            errors.Add(PublicContentErrors.UnsupportedLanguage(nameof(SourceLanguage)));
        }
    }

    private static void ValidateVariantLanguage(
        ValidationErrors errors,
        string field,
        PublicContentVariant variant,
        PublicContentLanguage expectedLanguage)
    {
        if (variant.Language != expectedLanguage)
        {
            errors.Add(PublicContentErrors.VariantLanguageMismatch(field, expectedLanguage, variant.Language));
        }
    }
}
