using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SharedKernel.BuildingBlocks;
using SharedKernel.Domain;
using SharedKernel.Results;

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
        SourceLanguage = PublicContentCultures.EnglishUnitedStates;
    }

    private EditablePublicContent(
        Guid id,
        PublicContentKey key,
        ContentCulture sourceLanguage,
        IEnumerable<PublicContentVariant> variants,
        PublicContentPublicationState publicationState)
    {
        Id = id;
        ContentKey = key;
        SourceLanguage = sourceLanguage;
        _variants.AddRange(variants);
        PublicationState = publicationState;
    }

    /// <summary>
    /// Gets the unique content identifier.
    /// </summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>
    /// Gets the stable content key, such as a page or section identifier.
    /// </summary>
    public string Key => ContentKey.Value;

    /// <summary>Gets the validated public-content identity.</summary>
    public PublicContentKey ContentKey { get; private set; } = default!;

    /// <summary>
    /// Gets the culture the editor originally wrote the content in.
    /// </summary>
    public ContentCulture SourceLanguage
    {
        get; private set;
    }

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
    public PublicContentPublicationState PublicationState
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether this content can be served on public endpoints.
    /// </summary>
    public bool IsPubliclyVisible => PublicationState == PublicContentPublicationState.Published;

    /// <summary>
    /// Gets a value indicating whether all variants are approved for publication.
    /// </summary>
    public bool CanPublish => _variants.All(variant => !variant.RequiresHumanReview);

    /// <summary>
    /// Creates editable public website content with supported language variants.
    /// </summary>
    /// <param name="key">The stable content key.</param>
    /// <param name="sourceLanguage">The source culture entered by the editor; it must be supported.</param>
    /// <param name="variants">The localized variants.</param>
    /// <returns>A result containing editable content when valid.</returns>
    public static Result<EditablePublicContent> Create(
        string key,
        ContentCulture? sourceLanguage,
        IEnumerable<PublicContentVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        var contentKey = PublicContentKey.Create(key);
        var variantsArray = variants.ToArray();
        var errors = new ValidationErrors();

        if (contentKey.IsFailure)
        {
            errors.Add(contentKey);
        }

        ValidateSupportedSourceLanguage(errors, sourceLanguage);
        ValidateVariants(errors, variantsArray);

        return errors.HasErrors
            ? errors.ToResult<EditablePublicContent>()
            : Result.Ok(new EditablePublicContent(
                Guid.CreateVersion7(),
                contentKey.Value,
                sourceLanguage!,
                variantsArray,
                GetInitialPublicationState(variantsArray)));
    }

    /// <summary>
    /// Publishes content after all language variants have passed human review.
    /// </summary>
    /// <returns>A result indicating whether publication was allowed.</returns>
    public Result Publish()
    {
        if (!CanPublish)
        {
            return PublicContentErrors.ReviewRequiredBeforePublishing();
        }

        PublicationState = PublicContentPublicationState.Published;
        return Result.Ok();
    }

    /// <summary>
    /// Publishes content when all variants are approved, otherwise leaves it in review.
    /// </summary>
    /// <returns>A result indicating whether auto-publication was allowed or skipped.</returns>
    public Result PublishIfReady()
    {
        return CanPublish ? Publish() : Result.Ok();
    }

    /// <summary>
    /// Replaces the editable language variants for the same content key.
    /// </summary>
    /// <param name="sourceLanguage">The source culture entered by the editor; it must be supported.</param>
    /// <param name="variants">The localized variants.</param>
    /// <returns>A result indicating whether replacement was allowed.</returns>
    public Result ReplaceVariants(
        ContentCulture? sourceLanguage,
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

        SourceLanguage = sourceLanguage!;
        _variants.Clear();
        _variants.AddRange(variantsArray);
        PublicationState = GetInitialPublicationState(variantsArray);

        return Result.Ok();
    }

    /// <summary>
    /// Replaces this content with another version for the same key.
    /// </summary>
    /// <param name="replacement">The replacement content.</param>
    /// <returns>A result indicating whether replacement was allowed.</returns>
    public Result ReplaceWith(EditablePublicContent replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        var replace = ReplaceVariants(replacement.SourceLanguage, replacement.Variants);
        return replace.IsFailure switch
        {
            true => replace,
            false when replacement.IsPubliclyVisible => Publish(),
            _ => Result.Ok(),
        };
    }

    /// <summary>
    /// Resolves the approved public variant for a requested culture with <see cref="TranslationSet{T}.TryResolve"/>: the exact
    /// culture, its neutral parent, the first variant of the same language, then the default culture. Variants that still need
    /// human review are never chosen.
    /// </summary>
    /// <param name="requested">The requested culture, or <see langword="null"/> for the default.</param>
    /// <param name="variant">The approved public variant when one is found.</param>
    /// <returns><see langword="true"/> when an approved variant is found.</returns>
    public bool TryResolvePublicVariant(ContentCulture? requested, [MaybeNullWhen(false)] out PublicContentVariant variant)
    {
        var approved = TranslationSet.Create(_variants.Where(item => !item.RequiresHumanReview), item => item.Language);
        if (approved.IsFailure)
        {
            variant = null;
            return false;
        }

        return approved.Value.TryResolve(requested, PublicContentCultures.Supported.Default, out variant);
    }

    private static PublicContentPublicationState GetInitialPublicationState(IEnumerable<PublicContentVariant> variants)
    {
        return variants.Any(variant => variant.RequiresHumanReview)
            ? PublicContentPublicationState.ReviewRequired
            : PublicContentPublicationState.Draft;
    }

    private static void ValidateSupportedSourceLanguage(
        ValidationErrors errors,
        ContentCulture? sourceLanguage)
    {
        if (!PublicContentCultures.IsSupported(sourceLanguage))
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

        foreach (var language in PublicContentCultures.Supported.Cultures
            .Where(language => variants.All(variant => variant.Language != language)))
        {
            errors.Add(PublicContentErrors.MissingVariantLanguage(nameof(Variants), language));
        }
    }
}
