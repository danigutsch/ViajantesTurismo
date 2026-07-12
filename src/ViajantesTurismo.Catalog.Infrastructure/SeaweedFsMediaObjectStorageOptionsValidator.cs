using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class SeaweedFsMediaObjectStorageOptionsValidator : IValidateOptions<SeaweedFsMediaObjectStorageOptions>
{
    private static TimeSpan MaximumSupportedTimeout { get; } = TimeSpan.FromMilliseconds(int.MaxValue);

    public ValidateOptionsResult Validate(string? name, SeaweedFsMediaObjectStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options switch
        {
            { Endpoint: null } => ValidateOptionsResult.Fail("SeaweedFS endpoint must be provided."),
            { Endpoint.IsAbsoluteUri: false } => ValidateOptionsResult.Fail("SeaweedFS endpoint must be absolute."),
            { Endpoint.Scheme: not ("http" or "https") } => ValidateOptionsResult.Fail("SeaweedFS endpoint must use HTTP or HTTPS."),
            var value when string.IsNullOrWhiteSpace(value.Bucket) => ValidateOptionsResult.Fail("SeaweedFS bucket must be provided."),
            var value when string.IsNullOrWhiteSpace(value.AccessKey) => ValidateOptionsResult.Fail("SeaweedFS access key must be provided."),
            var value when string.IsNullOrWhiteSpace(value.SecretKey) => ValidateOptionsResult.Fail("SeaweedFS secret key must be provided."),
            { BucketProvisioningTimeout: var value } when value <= TimeSpan.Zero => ValidateOptionsResult.Fail("SeaweedFS bucket provisioning timeout must be positive."),
            { BucketProvisioningTimeout: var value } when value > MaximumSupportedTimeout => ValidateOptionsResult.Fail("SeaweedFS bucket provisioning timeout exceeds the supported cancellation timeout."),
            { PublicBaseUri: null } => ValidateOptionsResult.Fail("SeaweedFS public base URI must be provided."),
            _ => ValidateOptionsResult.Success
        };
    }
}
