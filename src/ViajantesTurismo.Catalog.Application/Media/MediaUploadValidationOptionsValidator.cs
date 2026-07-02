using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Application.Media;

internal sealed class MediaUploadValidationOptionsValidator : IValidateOptions<MediaUploadValidationOptions>
{
    public ValidateOptionsResult Validate(string? name, MediaUploadValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxLengthBytes <= 0)
        {
            return ValidateOptionsResult.Fail("Media upload maximum length must be greater than zero.");
        }

        if (options.AllowedExtensionsByContentType.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one media upload content type must be allowed.");
        }

        return ValidateOptionsResult.Success;
    }
}
