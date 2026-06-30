using Microsoft.AspNetCore.WebUtilities;

namespace ViajantesTurismo.Public.Web;

internal static class PublicCultureQuery
{
    public static string GetDocumentLanguage(string uri)
    {
        return GetRequestedCulture(uri) ?? "en";
    }

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

    private static string? NormalizeCulture(string? culture)
    {
        return culture?.Trim().ToUpperInvariant() switch
        {
            "EN-US" => "en-US",
            "PT-BR" => "pt-BR",
            _ => null
        };
    }
}
