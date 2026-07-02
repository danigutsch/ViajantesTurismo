using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class LocalMediaObjectStorageOptionsValidator : IValidateOptions<LocalMediaObjectStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, LocalMediaObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            return ValidateOptionsResult.Fail("Local media storage root path must be provided.");
        }

        if (options.PublicBaseUri is null || options.PublicBaseUri.OriginalString.Length == 0)
        {
            return ValidateOptionsResult.Fail("Local media storage public base URI must be provided.");
        }

        return ValidateOptionsResult.Success;
    }
}
