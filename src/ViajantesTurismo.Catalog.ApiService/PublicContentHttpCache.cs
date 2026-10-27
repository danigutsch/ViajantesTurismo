using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>Cache area, tag and header settings for public content responses.</summary>
internal static class PublicContentHttpCache
{
    /// <summary>Cache invalidation area name.</summary>
    public const string Area = "public-content";

    /// <summary>Query key carrying the response culture.</summary>
    public const string CultureQueryKey = "culture";

    /// <summary>Cache tag applied to responses.</summary>
    public const string Tag = "public-content";

    /// <summary>Time public responses stay fresh.</summary>
    public static readonly TimeSpan Freshness = TimeSpan.FromSeconds(60);

    private const string InvalidCultureCacheKey = "invalid";

    private const string LanguageQueryKey = "language";

    private static readonly string PathPrefix =
        $"/api/{CatalogOpenApiDocuments.CurrentApiVersion.RouteSegment}/public/catalog/content";

    /// <summary>Sets public cache headers using the freshness window.</summary>
    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, Freshness);
    }

    /// <summary>Maps the legacy language query key onto the culture key for public content requests.</summary>
    public static IApplicationBuilder UsePublicContentLanguageQueryAlias(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (httpContext, next) =>
        {
            if (httpContext.Request.Path.StartsWithSegments(PathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                HttpCacheCultures.NormalizeQueryAliases(httpContext, CultureQueryKey, LanguageQueryKey, InvalidCultureCacheKey);
            }

            await next(httpContext);
        });
    }
}
