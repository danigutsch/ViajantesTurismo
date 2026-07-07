using SharedKernel.AI;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Generates and stores human-review-required accessibility text drafts for media images.
/// </summary>
public sealed class MediaImageAccessibilityDraftService(
    IPublicMediaImageStore imageStore,
    IMediaObjectStore objectStore,
    IImageTextGenerator imageTextGenerator)
{
    /// <summary>
    /// Generates and stores AI-assisted draft accessibility text.
    /// </summary>
    /// <param name="imageId">The media image identifier.</param>
    /// <param name="input">The draft input.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated media image.</returns>
    public async ValueTask<Result<PublicMediaImage>> GenerateDraft(Guid imageId, MediaImageAccessibilityDraftInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (imageId == Guid.Empty)
        {
            return Result.Invalid<PublicMediaImage>("Media image identifier is required.", nameof(imageId), "Media image identifier is required.");
        }

        if (input.Language == PublicContentLanguage.None)
        {
            return Result.Invalid<PublicMediaImage>("Draft language is required.", nameof(input.Language), "Draft language is required.");
        }

        if ((input.Latitude is null) != (input.Longitude is null))
        {
            return Result.Invalid<PublicMediaImage>("Latitude and longitude must be supplied together.", nameof(input.Latitude), "Latitude and longitude must be supplied together.");
        }

        var image = await imageStore.GetImage(imageId, ct).ConfigureAwait(false);
        if (image is null)
        {
            return Result.NotFound<PublicMediaImage>("Media image was not found.");
        }

        using var source = await objectStore.OpenRead(image.SourceObjectKey, ct).ConfigureAwait(false);
        ImageTextGenerationResult draft;
        try
        {
            draft = await imageTextGenerator.GenerateImageText(
                new ImageTextGenerationRequest
                {
                    Image = source.Content,
                    ContentType = source.ContentType,
                    Language = ToLanguageTag(input.Language),
                    Context = input.Context,
                    Latitude = input.Latitude,
                    Longitude = input.Longitude
                },
                ct).ConfigureAwait(false);
        }
        catch (ImageTextGenerationException)
        {
            return Result.Unavailable<PublicMediaImage>("AI image text generation is unavailable.");
        }
        catch (InvalidOperationException)
        {
            return Result.Unavailable<PublicMediaImage>("AI image text generation is unavailable.");
        }

        var result = image.SetAiDraftAccessibilityText(input.Language, draft.AltText, draft.Caption);
        if (result.IsFailure)
        {
            return result.ConvertError<PublicMediaImage>();
        }

        await imageStore.Upsert(image, ct).ConfigureAwait(false);
        return Result.Ok(image);
    }

    private static string ToLanguageTag(PublicContentLanguage language)
    {
        return language switch
        {
            PublicContentLanguage.EnUs => "en-US",
            PublicContentLanguage.PtBr => "pt-BR",
            _ => "und"
        };
    }
}
