using System.Globalization;
using SharedKernel.ImageProcessing;
using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Processes stored original media images into deterministic public variants.
/// </summary>
public sealed class MediaImageProcessingRequestedIntegrationEventConsumer(
    IMediaObjectStore objectStore,
    IPublicMediaImageStore imageStore) : IIntegrationEventHandler<MediaImageProcessingRequestedIntegrationEvent>
{
    private static readonly ImageProcessingLimits Limits = ImageProcessingLimits.WebDefault;

    /// <inheritdoc />
    public async ValueTask Handle(MediaImageProcessingRequestedIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentException.ThrowIfNullOrWhiteSpace(notification.SourceObjectKey);

        var image = await imageStore.GetImage(notification.MediaImageId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Media image metadata must exist before processing starts.");

        if (image.ProcessingStatus == MediaImageProcessingStatus.Ready && image.ResponsiveVariants.Count > 0)
        {
            return;
        }

        try
        {
            using var original = await objectStore.OpenRead(notification.SourceObjectKey, ct).ConfigureAwait(false);
            var result = MagickImageProcessor.Process(
                new ImageProcessingRequest(original.Content, CreateVariantRequests(), Limits),
                ct);
            var variants = new List<MediaImageResponsiveVariant>(result.Variants.Count);

            foreach (var variant in result.Variants)
            {
                using var content = new MemoryStream(variant.Content.ToArray());
                var objectKey = CreateVariantObjectKey(notification.MediaImageId, notification.ProcessingVersion, variant);
                var stored = await objectStore.Put(
                    new MediaObjectWriteRequest(
                        objectKey,
                        content,
                        GetContentType(variant.Format),
                        variant.Content.Length),
                    ct).ConfigureAwait(false);
                if (IsResponsiveVariant(variant))
                {
                    variants.Add(new MediaImageResponsiveVariant(
                        stored.PublicUri,
                        variant.Width,
                        variant.Height,
                        stored.ContentType,
                        stored.Length,
                        variants.Count));
                }
            }

            await imageStore.Upsert(CloneWithProcessingResult(image, result, MediaImageProcessingStatus.Ready, variants), ct).ConfigureAwait(false);
        }
        catch (ImageProcessingException)
        {
            await imageStore.Upsert(CloneWithProcessingResult(
                image,
                new ImageProcessingResult(image.Dimensions.Width, image.Dimensions.Height, []),
                MediaImageProcessingStatus.Failed,
                []), ct).ConfigureAwait(false);
        }
    }

    private static IReadOnlyList<ImageVariantRequest> CreateVariantRequests()
    {
        return
        [
            new("thumb-webp", ImageOutputFormat.WebP, 320, 80, 320),
            new("icon-ico", ImageOutputFormat.Ico, 32, 90, 32),
            new("320-avif", ImageOutputFormat.Avif, 320, 55),
            new("320-webp", ImageOutputFormat.WebP, 320, 80),
            new("320-jpeg", ImageOutputFormat.Jpeg, 320, 82),
            new("640-avif", ImageOutputFormat.Avif, 640, 55),
            new("640-webp", ImageOutputFormat.WebP, 640, 80),
            new("640-jpeg", ImageOutputFormat.Jpeg, 640, 82),
            new("960-avif", ImageOutputFormat.Avif, 960, 55),
            new("960-webp", ImageOutputFormat.WebP, 960, 80),
            new("960-jpeg", ImageOutputFormat.Jpeg, 960, 82),
            new("1280-avif", ImageOutputFormat.Avif, 1280, 55),
            new("1280-webp", ImageOutputFormat.WebP, 1280, 80),
            new("1280-jpeg", ImageOutputFormat.Jpeg, 1280, 82),
            new("1920-avif", ImageOutputFormat.Avif, 1920, 55),
            new("1920-webp", ImageOutputFormat.WebP, 1920, 80),
            new("1920-jpeg", ImageOutputFormat.Jpeg, 1920, 82),
        ];
    }

    private static PublicMediaImage CloneWithProcessingResult(
        PublicMediaImage image,
        ImageProcessingResult result,
        MediaImageProcessingStatus status,
        IReadOnlyList<MediaImageResponsiveVariant> variants)
    {
        return new PublicMediaImage(
            new PublicMediaImageMetadata
            {
                Id = image.Id,
                SourceUri = image.SourceUri,
                Checksum = image.Checksum,
                ContentType = image.ContentType,
                FileSizeBytes = image.FileSizeBytes,
                Dimensions = new MediaImageDimensions(result.Width, result.Height),
                ProcessingStatus = status,
                AltText = image.AltText,
                Caption = image.Caption,
                Attribution = image.Attribution,
                Copyright = image.Copyright,
            },
            variants,
            image.Tags,
            image.TourLinks);
    }

    private static string CreateVariantObjectKey(Guid mediaImageId, int processingVersion, ProcessedImageVariant variant)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"media/{mediaImageId:N}/v{processingVersion}/{variant.Name}.{GetFileExtension(variant.Format)}");

    private static string GetContentType(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Avif => "image/avif",
        ImageOutputFormat.WebP => "image/webp",
        ImageOutputFormat.Jpeg => "image/jpeg",
        ImageOutputFormat.Ico => "image/x-icon",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported image output format.")
    };

    private static bool IsResponsiveVariant(ProcessedImageVariant variant)
        => variant.Format is ImageOutputFormat.Avif or ImageOutputFormat.WebP or ImageOutputFormat.Jpeg
            && !variant.Name.StartsWith("thumb-", StringComparison.Ordinal)
            && !variant.Name.StartsWith("icon-", StringComparison.Ordinal);

    private static string GetFileExtension(ImageOutputFormat format) => format switch
    {
        ImageOutputFormat.Avif => "avif",
        ImageOutputFormat.WebP => "webp",
        ImageOutputFormat.Jpeg => "jpg",
        ImageOutputFormat.Ico => "ico",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported image output format.")
    };
}
