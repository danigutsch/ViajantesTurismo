using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>Applies HTTP cache headers for Public Web pages and responses.</summary>
internal static class PublicWebHttpCache
{
    /// <summary>How long published content may be served from shared caches.</summary>
    public static readonly TimeSpan PublishedContentFreshness = TimeSpan.FromSeconds(60);

    /// <summary>Query key carrying the requested culture.</summary>
    public const string CultureQueryKey = "culture";

    /// <summary>Legacy query key alias for the requested culture.</summary>
    public const string LanguageQueryKey = "language";

    /// <summary>Adds middleware that normalizes the culture query alias and sets cache headers on public pages.</summary>
    public static IApplicationBuilder UsePublicWebCacheHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (httpContext, next) =>
        {
            if (IsPublicPageRequest(httpContext))
            {
                NormalizeCultureQueryAlias(httpContext);
            }

            httpContext.Response.OnStarting(static state =>
            {
                var context = (HttpContext)state;
                if (IsErrorRequest(context)
                    || (IsPublicPageRequest(context) && context.Response.StatusCode >= StatusCodes.Status400BadRequest))
                {
                    SetNoStore(context);
                }
                else if (IsPublicPageRequest(context))
                {
                    SetPublishedContent(context);
                }

                return Task.CompletedTask;
            }, httpContext);

            await next(httpContext);
        });
    }

    /// <summary>Marks the response as not cacheable.</summary>
    public static void SetNoStore(HttpContext httpContext)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
    }

    /// <summary>Sets a 503 status with no-store caching when the response has not started.</summary>
    public static void SetServiceUnavailableNoStore(HttpContext? httpContext)
    {
        if (httpContext is null || httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        SetNoStore(httpContext);
    }

    /// <summary>Marks the response as publicly cacheable for the published content freshness window.</summary>
    public static void SetPublishedContent(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, PublishedContentFreshness);
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

    private static void NormalizeCultureQueryAlias(HttpContext httpContext)
    {
        HttpCacheCultures.NormalizeQueryAliases(httpContext, CultureQueryKey, LanguageQueryKey);
    }
}
