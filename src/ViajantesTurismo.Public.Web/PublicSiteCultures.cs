using SharedKernel.BuildingBlocks;
using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>
/// The languages of the public site. URLs carry the short culture (<c>/pt/...</c>, <c>/en/...</c>); the page language,
/// <c>hreflang</c> and the Catalog content API use the regional content language (<c>pt-BR</c>, <c>en-US</c>).
/// </summary>
internal static class PublicSiteCultures
{
    /// <summary>English, the default culture of the site.</summary>
    public static readonly ContentCulture English = ContentCulture.Restore("en");

    /// <summary>Brazilian Portuguese, written as <c>pt</c> in URLs.</summary>
    public static readonly ContentCulture Portuguese = ContentCulture.Restore("pt");

    /// <summary>Gets the URL cultures of the site, English first and by default.</summary>
    public static SupportedCultures Supported { get; } = SupportedCultures.Create(English, [English, Portuguese]).Value;

    /// <summary>
    /// Path prefixes that are not pages and therefore never carry a culture: the error and health endpoints and the
    /// catalog media proxy. Files (a last segment with a <c>.</c>) are skipped by the culture redirect itself.
    /// </summary>
    public static IReadOnlyCollection<PathString> UnlocalizedPrefixes => Unlocalized;

    private static readonly PathString[] Unlocalized = [new("/Error"), new("/health"), new("/alive"), new("/catalog")];

    /// <summary>Maps a URL culture to the content language the Catalog API and the page metadata use.</summary>
    /// <param name="urlCulture">A supported URL culture.</param>
    /// <returns><c>pt-BR</c> for <c>pt</c> and <c>en-US</c> for <c>en</c>.</returns>
    public static ContentCulture ContentLanguage(ContentCulture urlCulture)
    {
        ArgumentNullException.ThrowIfNull(urlCulture);

        var contentLanguage = HttpCacheCultures.Normalize(urlCulture.Value)
            ?? throw new ArgumentException("The culture is not a public site culture.", nameof(urlCulture));
        return ContentCulture.Restore(contentLanguage);
    }

    /// <summary>Gets the name of a URL culture in its own language, for the language switcher.</summary>
    public static string NativeName(ContentCulture urlCulture)
    {
        ArgumentNullException.ThrowIfNull(urlCulture);

        return urlCulture.Equals(Portuguese) ? "Português" : "English";
    }

    /// <summary>Builds the local address of a page in a culture.</summary>
    /// <param name="urlCulture">A supported URL culture.</param>
    /// <param name="escapedPath">The page path without culture, URL-escaped and starting with <c>/</c>.</param>
    /// <returns>The path with the culture segment, such as <c>/pt/group-bike-tours</c> or <c>/en/</c>.</returns>
    public static string Localize(ContentCulture urlCulture, string escapedPath)
    {
        ArgumentNullException.ThrowIfNull(urlCulture);
        ArgumentException.ThrowIfNullOrWhiteSpace(escapedPath);

        return "/" + urlCulture.Value + escapedPath;
    }
}
