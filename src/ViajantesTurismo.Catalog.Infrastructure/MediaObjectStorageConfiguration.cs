namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Names the configuration sections that select the durable media object store.
/// </summary>
internal static class MediaObjectStorageConfiguration
{
    /// <summary>
    /// Gets the SeaweedFS configuration section name.
    /// </summary>
    public const string SectionName = "Catalog:MediaObjectStorage:SeaweedFs";

    /// <summary>
    /// Gets the production S3 configuration section name.
    /// </summary>
    public const string S3SectionName = "Catalog:MediaObjectStorage:S3";
}
