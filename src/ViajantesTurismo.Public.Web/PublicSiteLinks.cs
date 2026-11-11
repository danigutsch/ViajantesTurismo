using PandaBear.AspNetCore.Seo;
using PandaBear.Blazor.Site;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Public.Web;

/// <summary>
/// Builds the absolute addresses of a page under the configured canonical origin: one per culture, plus the address
/// without culture that redirects each visitor to their language and serves as <c>x-default</c>.
/// </summary>
/// <param name="options">The sitemap options that hold the canonical origin.</param>
internal sealed class PublicSiteLinks(PublicWebSitemapOptions options)
{
    private readonly Uri origin = SitemapCanonicalOrigin.Parse(options.CanonicalOrigin);

    /// <summary>Gets the canonical address of a page in a culture.</summary>
    /// <param name="urlCulture">The URL culture.</param>
    /// <param name="escapedPath">The page path without culture, URL-escaped and starting with <c>/</c>.</param>
    public Uri Canonical(ContentCulture urlCulture, string escapedPath) =>
        new(origin, PublicSiteCultures.Localize(urlCulture, escapedPath));

    /// <summary>Gets the address without culture, which redirects to the visitor's language.</summary>
    public Uri Default(string escapedPath) => new(origin, escapedPath);

    /// <summary>Gets the page's address in every culture, labelled with its content language.</summary>
    public IReadOnlyList<AlternateLink> Alternates(string escapedPath) => [.. PublicSiteCultures.Supported.Cultures.Select(culture => new AlternateLink(PublicSiteCultures.ContentLanguage(culture), Canonical(culture, escapedPath)))];

    /// <summary>Gets the sitemap entries of a page: one per culture, each listing every language version and <c>x-default</c>.</summary>
    public IEnumerable<SitemapEntry> SitemapEntries(string escapedPath, DateTimeOffset? lastModified = null)
    {
        SitemapAlternate[] alternates =
        [
            .. Alternates(escapedPath).Select(alternate => new SitemapAlternate(alternate.Culture.Value, alternate.Href)),
            SitemapAlternate.ForDefault(Default(escapedPath))
        ];

        return PublicSiteCultures.Supported.Cultures
            .Select(culture => new SitemapEntry(Canonical(culture, escapedPath), lastModified, alternates));
    }

    /// <summary>Gets an absolute address of a local path under the canonical origin.</summary>
    public Uri Absolute(string localPath) => new(origin, localPath);
}
