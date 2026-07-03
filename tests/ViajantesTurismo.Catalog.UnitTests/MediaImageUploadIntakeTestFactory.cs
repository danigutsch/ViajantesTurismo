using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class MediaImageUploadIntakeTestFactory
{
    public static MediaImageUploadIntake Create(
        IMediaUploadScanner scanner,
        IMediaObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        MediaUploadValidationOptions? options = null)
    {
        var validationOptions = options ?? new MediaUploadValidationOptions();

        return new MediaImageUploadIntake(
            new MediaUploadValidator(validationOptions),
            scanner,
            objectStore,
            imageStore,
            Options.Create(validationOptions));
    }
}
