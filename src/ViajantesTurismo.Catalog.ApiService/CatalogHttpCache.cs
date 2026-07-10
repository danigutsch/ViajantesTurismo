using SharedKernel.HttpCaching.AspNetCore;

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

    private static readonly TimeSpan StaleWhileRevalidate = TimeSpan.FromSeconds(300);

    internal const string CultureQueryKey = "culture";

    private const string InvalidCultureCacheKey = "invalid";

    private const string LanguageQueryKey = "language";

    private static readonly string PublicContentPathPrefix =
        $"/api/{CatalogOpenApiDocuments.CurrentApiVersion.RouteSegment}/public/catalog/content";

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
        HttpCacheHeaders.SetPublic(httpContext, PublicFreshness, StaleWhileRevalidate);
    }

    public static void SetNoStore(HttpContext httpContext)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
    }

    private static void NormalizeCultureQueryAlias(HttpContext httpContext)
    {
        HttpCacheCultures.NormalizeQueryAliases(httpContext, CultureQueryKey, LanguageQueryKey, InvalidCultureCacheKey);
    }
}
