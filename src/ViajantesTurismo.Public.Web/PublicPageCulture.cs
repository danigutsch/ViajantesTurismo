using PandaBear.AspNetCore.Localization;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Public.Web;

/// <summary>The culture of the page being rendered and its path without the culture segment, cascaded from the host page.</summary>
/// <param name="UrlCulture">The culture of the URL (<c>pt</c> or <c>en</c>).</param>
/// <param name="PagePath">The request path without the culture segment, for the language switcher.</param>
internal sealed record PublicPageCulture(ContentCulture UrlCulture, PathString PagePath)
{
    /// <summary>Gets the content language of the page (<c>pt-BR</c> or <c>en-US</c>).</summary>
    public ContentCulture ContentLanguage => PublicSiteCultures.ContentLanguage(UrlCulture);

    /// <summary>Gets whether built-in page text should use Brazilian Portuguese.</summary>
    public bool IsPortuguese => UrlCulture.Equals(PublicSiteCultures.Portuguese);

    /// <summary>Reads the culture of a request path, or the default culture when the path has none.</summary>
    public static PublicPageCulture FromPath(PathString path)
    {
        return CulturePath.TryParse(path, PublicSiteCultures.Supported, out var culture, out var remainder)
            ? new PublicPageCulture(culture, remainder)
            : new PublicPageCulture(PublicSiteCultures.Supported.Default, path);
    }

    /// <summary>Builds the local address of a page in this culture.</summary>
    /// <param name="escapedPath">The page path without culture, URL-escaped and starting with <c>/</c>.</param>
    public string Localize(string escapedPath) => PublicSiteCultures.Localize(UrlCulture, escapedPath);

    /// <summary>Builds the address of this page in another culture.</summary>
    public string Switch(ContentCulture culture) =>
        CulturePath.Prepend(PagePath.HasValue ? PagePath : new PathString("/"), culture, PublicSiteCultures.Supported).ToUriComponent();
}
