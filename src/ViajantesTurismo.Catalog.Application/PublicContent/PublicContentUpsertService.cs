using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.PublicContent;

/// <summary>
/// Creates or updates editable public content and persists its publication state.
/// </summary>
public sealed class PublicContentUpsertService(IPublicContentStore store)
{
    /// <summary>
    /// Creates or updates public content from an editor request.
    /// </summary>
    /// <param name="key">The stable public content key.</param>
    /// <param name="request">The editor request.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The saved public content when the request is valid.</returns>
    public async ValueTask<Result<EditablePublicContent>> Upsert(
        string key,
        UpsertPublicContentRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Variants is null)
        {
            return Result.Invalid<EditablePublicContent>(
                "Public content variants must be provided.",
                nameof(UpsertPublicContentRequest.Variants),
                "Variants are required.");
        }

        var variantResults = request.Variants.Select(CreateVariant).ToArray();
        var validationErrors = new ValidationErrors();
        foreach (var variantResult in variantResults.Where(variantResult => variantResult.IsFailure))
        {
            validationErrors.Add(variantResult);
        }

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToResult<EditablePublicContent>();
        }

        var content = EditablePublicContent.Create(
            key,
            ToDomainLanguage(request.SourceLanguage),
            variantResults.Select(variantResult => variantResult.Value));
        if (content.IsFailure)
        {
            return content;
        }

        var publish = content.Value.PublishIfReady();
        if (publish.IsFailure)
        {
            return publish.ConvertError<EditablePublicContent>();
        }

        await store.SaveContent(content.Value, ct).ConfigureAwait(false);
        return content;
    }

    private static Result<PublicContentVariant> CreateVariant(PublicContentVariantDto? variant)
    {
        if (variant is null)
        {
            return Result.Invalid<PublicContentVariant>(
                "Public content variants cannot contain null entries.",
                nameof(UpsertPublicContentRequest.Variants),
                "Variants cannot contain null entries.");
        }

        return PublicContentVariant.Create(
            ToDomainLanguage(variant.Language),
            variant.Title,
            variant.Body,
            variant.SeoTitle,
            variant.MetaDescription,
            variant.ShareSummary,
            variant.RequiresHumanReview);
    }

    private static PublicContentLanguage ToDomainLanguage(PublicContentLanguageDto language)
    {
        return language == PublicContentLanguageDto.None || !Enum.IsDefined(language)
            ? PublicContentLanguage.None
            : (PublicContentLanguage)(int)language;
    }
}
