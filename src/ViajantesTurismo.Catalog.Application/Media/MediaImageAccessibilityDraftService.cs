using PandaBear.ObjectStorage;
using System.Diagnostics;
using PandaBear.AI;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Generates and stores human-review-required accessibility text drafts for media images.
/// </summary>
public sealed class MediaImageAccessibilityDraftService(
    IPublicMediaImageStore imageStore,
    IObjectStore objectStore,
    IImageTextGenerator imageTextGenerator,
    TimeProvider? timeProvider = null,
    MediaImageAccessibilityGenerationProvenance? provenance = null)
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

        ObjectReadResult source;
        try
        {
            source = await objectStore.OpenRead(image.SourceObjectKey, ct).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            return Result.NotFound<PublicMediaImage>("Media image source object was not found.");
        }
        catch (FileNotFoundException)
        {
            return Result.NotFound<PublicMediaImage>("Media image source object was not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return Result.NotFound<PublicMediaImage>("Media image source object was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Unavailable<PublicMediaImage>("Media image source object is unavailable.");
        }
        catch (IOException)
        {
            return Result.Unavailable<PublicMediaImage>("Media image source object is unavailable.");
        }

        using var readableSource = source;
        var started = Stopwatch.GetTimestamp();
        var outcome = "failure";
        MediaAccessibilityTelemetry.RecordAttempt(input.Language);
        using var activity = CatalogTelemetry.ActivitySource.StartActivity("catalog.media_accessibility.generate");
        activity?.SetTag("catalog.media_accessibility.configured_provider", provenance?.Provider ?? "unreported");
        activity?.SetTag("gen_ai.request.model", provenance?.RequestedModel ?? "unreported");
        activity?.SetTag("catalog.media_accessibility.adapter_version", typeof(IImageTextGenerator).Assembly.GetName().Version?.ToString() ?? "unreported");
        activity?.SetTag("catalog.media_accessibility.prompt_version", provenance?.PromptVersion ?? "unreported");
        activity?.SetTag("catalog.media_accessibility.config_version", provenance?.ConfigurationVersion ?? "unreported");
        activity?.SetTag("gen_ai.response.model", "unreported");
        try
        {
            ImageTextGenerationResult draft;
            try
            {
                draft = await imageTextGenerator.GenerateImageText(
                    new ImageTextGenerationRequest
                    {
                        Image = readableSource.Content,
                        ContentType = readableSource.ContentType,
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

            var result = image.SetAiDraftAccessibilityText(input.Language, draft.AltText, draft.Caption, (timeProvider ?? TimeProvider.System).GetUtcNow());
            if (result.IsFailure)
            {
                return result.ConvertError<PublicMediaImage>();
            }

            await imageStore.Upsert(image, ct).ConfigureAwait(false);
            outcome = "success";
            return Result.Ok(image);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            activity?.SetTag(CatalogTelemetry.TagOutcome, outcome);
            activity?.SetStatus(outcome == "success" ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            MediaAccessibilityTelemetry.RecordCompletion(outcome, input.Language, started);
        }
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
