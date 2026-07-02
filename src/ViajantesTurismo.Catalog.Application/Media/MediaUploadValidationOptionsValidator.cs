using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Application.Media;

internal sealed class MediaUploadValidationOptionsValidator : IValidateOptions<MediaUploadValidationOptions>
{
    public ValidateOptionsResult Validate(string? name, MediaUploadValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options switch
        {
            { MaxLengthBytes: <= 0 } => ValidateOptionsResult.Fail("Media upload maximum length must be greater than zero."),
            { AllowedExtensionsByContentType.Count: 0 } => ValidateOptionsResult.Fail("At least one media upload content type must be allowed."),
            _ => ValidateOptionsResult.Success
        };
    }
}
