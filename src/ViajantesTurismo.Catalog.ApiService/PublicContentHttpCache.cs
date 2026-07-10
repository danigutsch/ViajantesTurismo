using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class PublicContentHttpCache
{
    public const string Area = "public-content";

    public const string CultureQueryKey = "culture";

    public const string Tag = "public-content";

    public static readonly TimeSpan Freshness = TimeSpan.FromSeconds(60);

    private const string InvalidCultureCacheKey = "invalid";

    private const string LanguageQueryKey = "language";

    private static readonly TimeSpan StaleWhileRevalidate = TimeSpan.FromSeconds(300);

    private static readonly string PathPrefix =
        $"/api/{CatalogOpenApiDocuments.CurrentApiVersion.RouteSegment}/public/catalog/content";

    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, Freshness, StaleWhileRevalidate);
    }

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
