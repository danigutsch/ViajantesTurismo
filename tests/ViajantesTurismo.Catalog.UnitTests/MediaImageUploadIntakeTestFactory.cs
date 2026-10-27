using PandaBear.ObjectStorage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Builds <c>MediaImageUploadIntake</c> instances with test doubles and default options.</summary>
internal static class MediaImageUploadIntakeTestFactory
{
    /// <summary>Creates an intake with the given collaborators and options, using default outbox, logger and clock.</summary>
    public static MediaImageUploadIntake Create(
        IMediaUploadScanner scanner,
        IObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        MediaUploadValidationOptions options) =>
        Create(scanner, objectStore, imageStore, null, options);

    /// <summary>Creates an intake, defaulting any omitted outbox, options, logger or time provider.</summary>
    public static MediaImageUploadIntake Create(
        IMediaUploadScanner scanner,
        IObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        CapturingIntegrationEventOutbox? outbox = null,
        MediaUploadValidationOptions? options = null,
        ILogger<MediaImageUploadIntake>? logger = null,
        TimeProvider? timeProvider = null)
    {
        var validationOptions = options ?? new MediaUploadValidationOptions();

        return new MediaImageUploadIntake(
            new MediaUploadValidator(validationOptions),
            scanner,
            objectStore,
            imageStore,
            outbox ?? new CapturingIntegrationEventOutbox(),
            Options.Create(validationOptions),
            logger ?? NullLogger<MediaImageUploadIntake>.Instance, timeProvider ?? TimeProvider.System);
    }
}
