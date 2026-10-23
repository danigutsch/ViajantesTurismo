namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Configures the authenticated SeaweedFS S3-compatible media store.
/// </summary>
internal sealed class SeaweedFsMediaObjectStorageOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Catalog:MediaObjectStorage:SeaweedFs";

    /// <summary>
    /// Gets the production S3 configuration section name.
    /// </summary>
    public const string S3SectionName = "Catalog:MediaObjectStorage:S3";

    /// <summary>
    /// Gets or sets the S3 signing region.
    /// </summary>
    public string AuthenticationRegion { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets whether bucket names are placed in the endpoint path.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Gets or sets the private S3 endpoint.
    /// </summary>
    public Uri? Endpoint
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the private bucket name.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 access key.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 secret key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the adapter may create the bucket when it is absent.
    /// </summary>
    /// <remarks>
    /// Enable only for explicit local development. Production deployments must pre-provision the bucket.
    /// </remarks>
    public bool AutoProvisionBucket
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the maximum duration for local bucket auto-provisioning.
    /// </summary>
    public TimeSpan BucketProvisioningTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the public base URI for published media.
    /// </summary>
    public Uri PublicBaseUri { get; set; } = new("/media/", UriKind.Relative);
}
