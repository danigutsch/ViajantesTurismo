namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Configures local filesystem media object storage.
/// </summary>
public sealed class LocalMediaObjectStorageOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Catalog:MediaStorage";

    /// <summary>
    /// Gets or sets the local media root path.
    /// </summary>
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "media");

    /// <summary>
    /// Gets or sets the public base URI for stored media.
    /// </summary>
    public Uri PublicBaseUri { get; set; } = new("/media/", UriKind.Relative);
}
