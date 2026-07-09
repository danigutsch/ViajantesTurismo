namespace ViajantesTurismo.Public.Web;

internal static class PublicWebHttpCache
{
    public static readonly TimeSpan PublishedContentFreshness = TimeSpan.FromSeconds(60);

    public const string PublishedContentPolicy = "published-public-content";

    private const string PublicCacheControl = "public, max-age=60, stale-while-revalidate=300";

    private const string NoStoreCacheControl = "no-store";

    public static IApplicationBuilder UsePublicWebCacheHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (httpContext, next) =>
        {
            if (IsErrorRequest(httpContext))
            {
                SetNoStore(httpContext);
            }
            else if (IsPublicPageRequest(httpContext))
            {
                SetPublishedContent(httpContext);
            }

            await next(httpContext);
        });
    }

    public static void SetNoStore(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = NoStoreCacheControl;
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Expires = "0";
    }

    private static void SetPublishedContent(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = PublicCacheControl;
    }

    private static bool IsErrorRequest(HttpContext httpContext)
    {
        return HttpMethods.IsGet(httpContext.Request.Method)
            && httpContext.Request.Path.Equals("/Error", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicPageRequest(HttpContext httpContext)
    {
        var path = httpContext.Request.Path;
        return HttpMethods.IsGet(httpContext.Request.Method)
            && (path == "/"
                || path.StartsWithSegments("/group-bike-tours", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/gallery", StringComparison.OrdinalIgnoreCase));
    }
}
