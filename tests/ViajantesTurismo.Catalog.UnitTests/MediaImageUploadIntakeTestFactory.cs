using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class MediaImageUploadIntakeTestFactory
{
    public static MediaImageUploadIntake Create(
        IMediaUploadScanner scanner,
        IMediaObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        MediaUploadValidationOptions options) =>
        Create(scanner, objectStore, imageStore, null, options);

    public static MediaImageUploadIntake Create(
        IMediaUploadScanner scanner,
        IMediaObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        CapturingIntegrationEventOutbox? outbox = null,
        MediaUploadValidationOptions? options = null)
    {
        var validationOptions = options ?? new MediaUploadValidationOptions();

        return new MediaImageUploadIntake(
            new MediaUploadValidator(validationOptions),
            scanner,
            objectStore,
            imageStore,
            outbox ?? new CapturingIntegrationEventOutbox(),
            Options.Create(validationOptions),
            NullLogger<MediaImageUploadIntake>.Instance);
    }
}
