using Microsoft.AspNetCore.WebUtilities;
using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>Reads and normalizes the culture requested through the public site query string.</summary>
internal static class PublicCultureQuery
{
    /// <summary>Returns the requested culture for the page language, defaulting to English.</summary>
    public static string GetDocumentLanguage(string uri)
    {
        return GetRequestedCulture(uri) ?? "en";
    }

    /// <summary>Returns the normalized culture from the culture or language query parameter, or null when absent or unsupported.</summary>
    public static string? GetRequestedCulture(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri, UriKind.Absolute).Query);
        var requestedCulture = string.Empty;
        if (query.TryGetValue("culture", out var culture))
        {
            requestedCulture = culture.ToString();
        }
        else if (query.TryGetValue("language", out var language))
        {
            requestedCulture = language.ToString();
        }

        return NormalizeCulture(requestedCulture);
    }

    /// <summary>Normalizes a culture name using the shared HTTP caching culture rules, or null when unsupported.</summary>
    public static string? NormalizeCulture(string? culture)
    {
        return HttpCacheCultures.Normalize(culture);
    }
}
