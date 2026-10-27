using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Validates that local media storage has a root path and a public base URI.</summary>
internal sealed class LocalMediaObjectStorageOptionsValidator : IValidateOptions<LocalMediaObjectStorageOptions>
{
    /// <summary>Fails when the root path or public base URI is missing.</summary>
    public ValidateOptionsResult Validate(string? name, LocalMediaObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options switch
        {
            var value when string.IsNullOrWhiteSpace(value.RootPath) => ValidateOptionsResult.Fail("Local media storage root path must be provided."),
            { PublicBaseUri: null } => ValidateOptionsResult.Fail("Local media storage public base URI must be provided."),
            { PublicBaseUri.OriginalString.Length: 0 } => ValidateOptionsResult.Fail("Local media storage public base URI must be provided."),
            _ => ValidateOptionsResult.Success
        };
    }
}
