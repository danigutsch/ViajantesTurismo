using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Business-editable public website content with localized variants.
/// </summary>
[GenerateModelSupport(Identity = true)]
public sealed partial class EditablePublicContent : IAggregateRoot<Guid>
{
    private readonly List<PublicContentVariant> _variants = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    private EditablePublicContent()
    {
        Key = string.Empty;
    }

    private EditablePublicContent(
        Guid id,
        string key,
        PublicContentLanguage sourceLanguage,
        IEnumerable<PublicContentVariant> variants,
        PublicContentPublicationState publicationState)
    {
        Id = id;
        Key = key;
        SourceLanguage = sourceLanguage;
        _variants.AddRange(variants);
        PublicationState = publicationState;
    }

    /// <summary>
    /// Gets the unique content identifier.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the stable content key, such as a page or section identifier.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Gets the language originally entered by the editor.
    /// </summary>
    public PublicContentLanguage SourceLanguage { get; private set; }

    /// <summary>
    /// Gets the localized content variants.
    /// </summary>
    public IReadOnlyCollection<PublicContentVariant> Variants => _variants.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Gets the publication state for the content entry.
    /// </summary>
    public PublicContentPublicationState PublicationState { get; private set; }

    /// <summary>
    /// Creates editable public website content with supported language variants.
    /// </summary>
    /// <param name="key">The stable content key.</param>
    /// <param name="sourceLanguage">The source language entered by the editor.</param>
    /// <param name="variants">The localized variants.</param>
    /// <returns>A result containing editable content when valid.</returns>
    public static Result<EditablePublicContent> Create(
        string key,
        PublicContentLanguage sourceLanguage,
        IEnumerable<PublicContentVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        var sanitizedKey = StringSanitizer.Sanitize(key).ToUpperInvariant();
        var variantsArray = variants.ToArray();
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
        ValidateVariants(errors, variantsArray);

        return errors.HasErrors
            ? errors.ToResult<EditablePublicContent>()
            : Result.Ok(new EditablePublicContent(
                Guid.CreateVersion7(),
                sanitizedKey,
                sourceLanguage,
                variantsArray,
                GetInitialPublicationState(variantsArray)));
    }

    /// <summary>
    /// Publishes content after all language variants have passed human review.
    /// </summary>
    /// <returns>A result indicating whether publication was allowed.</returns>
    public Result Publish()
    {
        if (_variants.Any(variant => variant.RequiresHumanReview))
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
    /// <param name="variants">The localized variants.</param>
    /// <returns>A result indicating whether replacement was allowed.</returns>
    public Result ReplaceVariants(
        PublicContentLanguage sourceLanguage,
        IEnumerable<PublicContentVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        var variantsArray = variants.ToArray();
        var errors = new ValidationErrors();

        ValidateSupportedSourceLanguage(errors, sourceLanguage);
        ValidateVariants(errors, variantsArray);

        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        SourceLanguage = sourceLanguage;
        _variants.Clear();
        _variants.AddRange(variantsArray);
        PublicationState = GetInitialPublicationState(variantsArray);

        return Result.Ok();
    }

    private static PublicContentPublicationState GetInitialPublicationState(IEnumerable<PublicContentVariant> variants)
    {
        return variants.Any(variant => variant.RequiresHumanReview)
            ? PublicContentPublicationState.ReviewRequired
            : PublicContentPublicationState.Draft;
    }

    private static void ValidateSupportedSourceLanguage(
        ValidationErrors errors,
        PublicContentLanguage sourceLanguage)
    {
        if (sourceLanguage == PublicContentLanguage.None || !Enum.IsDefined(sourceLanguage))
        {
            errors.Add(PublicContentErrors.UnsupportedLanguage(nameof(SourceLanguage)));
        }
    }

    private static void ValidateVariants(ValidationErrors errors, IReadOnlyCollection<PublicContentVariant> variants)
    {
        foreach (var duplicate in variants
            .GroupBy(variant => variant.Language)
            .Where(group => group.Count() > 1))
        {
            errors.Add(PublicContentErrors.DuplicateVariantLanguage(nameof(Variants), duplicate.Key));
        }

        foreach (var language in GetSupportedLanguages()
            .Where(language => variants.All(variant => variant.Language != language)))
        {
            errors.Add(PublicContentErrors.MissingVariantLanguage(nameof(Variants), language));
        }
    }

    private static IEnumerable<PublicContentLanguage> GetSupportedLanguages()
    {
        return Enum.GetValues<PublicContentLanguage>().Where(language => language != PublicContentLanguage.None);
    }
}
