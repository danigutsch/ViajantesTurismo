namespace ViajantesTurismo.Public.Web;

/// <summary>
/// Configures canonical URLs emitted for public search crawlers.
/// </summary>
internal sealed class PublicWebSitemapOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "PublicWeb:Sitemap";

    /// <summary>
    /// Gets or sets the canonical public origin.
    /// </summary>
    public string CanonicalOrigin { get; set; } = string.Empty;
}
