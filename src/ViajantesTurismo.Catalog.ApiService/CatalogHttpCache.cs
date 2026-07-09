using Microsoft.Net.Http.Headers;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogHttpCache
{
    public static readonly TimeSpan PublicFreshness = TimeSpan.FromSeconds(60);

    public const string PublicCatalogTag = "public-catalog";

    public const string PublicContentTag = "public-content";

    public const string PublicThemeTag = "public-theme";

    public const string PublicCatalogArea = "public-catalog";

    public const string PublicContentArea = "public-content";

    public const string PublicThemeArea = "public-theme";

    private const string StaleWhileRevalidateDirective = "stale-while-revalidate";

    private const string StaleWhileRevalidateSeconds = "300";

    private const string PragmaNoCache = "no-cache";

    private const string ExpiredAtUnixEpochHttpDate = "Thu, 01 Jan 1970 00:00:00 GMT";

    private const string CultureQueryKey = "culture";

    private const string InvalidCultureCacheKey = "invalid";

    private const string LanguageQueryKey = "language";

    private const string PublicContentPathPrefix = "/public/catalog/content";

    public static IApplicationBuilder UsePublicContentLanguageQueryAlias(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (httpContext, next) =>
        {
            if (httpContext.Request.Path.StartsWithSegments(PublicContentPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                NormalizeCultureQueryAlias(httpContext);
            }

            await next(httpContext);
        });
    }

    public static void SetPublicHeaders(HttpContext httpContext)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = PublicFreshness,
            Extensions = { new NameValueHeaderValue(StaleWhileRevalidateDirective, StaleWhileRevalidateSeconds) }
        };
    }

    public static void SetNoStore(HttpContext httpContext)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoStore = true
        };
        httpContext.Response.Headers[HeaderNames.Pragma] = PragmaNoCache;
        httpContext.Response.Headers[HeaderNames.Expires] = ExpiredAtUnixEpochHttpDate;
    }

    private static void NormalizeCultureQueryAlias(HttpContext httpContext)
    {
        var hasCultureInput = httpContext.Request.Query.ContainsKey(CultureQueryKey)
            || httpContext.Request.Query.ContainsKey(LanguageQueryKey);
        if (!hasCultureInput)
        {
            return;
        }

        var canonicalCulture = httpContext.Request.Query.TryGetValue(CultureQueryKey, out var cultureValue)
            ? NormalizeCulture(cultureValue.ToString())
            : null;

        if (canonicalCulture is null
            && httpContext.Request.Query.TryGetValue(LanguageQueryKey, out var language))
        {
            canonicalCulture = NormalizeCulture(language.ToString());
        }

        var queryValues = new List<KeyValuePair<string, string?>>();
        foreach (var (key, values) in httpContext.Request.Query)
        {
            if (key.Equals(LanguageQueryKey, StringComparison.OrdinalIgnoreCase)
                || key.Equals(CultureQueryKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in values)
            {
                queryValues.Add(new KeyValuePair<string, string?>(key, value));
            }
        }

        queryValues.Add(new KeyValuePair<string, string?>(CultureQueryKey, canonicalCulture ?? InvalidCultureCacheKey));
        httpContext.Request.QueryString = QueryString.Create(queryValues);
    }

    private static string? NormalizeCulture(string? culture)
    {
        return culture?.Trim().ToUpperInvariant() switch
        {
            "EN-US" or "EN" => "en-US",
            "PT-BR" or "PT" => "pt-BR",
            _ => null
        };
    }
}
