using Microsoft.Net.Http.Headers;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebHttpCache
{
    public static readonly TimeSpan PublishedContentFreshness = TimeSpan.FromSeconds(60);

    public const string PublishedContentPolicy = "published-public-content";

    public const string CultureQueryKey = "culture";

    public const string LanguageQueryKey = "language";

    private const string StaleWhileRevalidateDirective = "stale-while-revalidate";

    private const string StaleWhileRevalidateSeconds = "300";

    private const string PragmaNoCache = "no-cache";

    private const string ExpiredAtUnixEpoch = "0";

    public static IServiceCollection AddPublicWebOutputCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddOutputCache(options =>
        {
            options.AddPolicy(
                PublishedContentPolicy,
                policy => policy.Expire(PublishedContentFreshness).SetVaryByQuery(CultureQueryKey, LanguageQueryKey));
        });
    }

    public static IApplicationBuilder UsePublicWebCacheHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (httpContext, next) =>
        {
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

    public static void SetNoStore(HttpContext httpContext)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoStore = true
        };
        httpContext.Response.Headers.Pragma = PragmaNoCache;
        httpContext.Response.Headers.Expires = ExpiredAtUnixEpoch;
    }

    private static void SetPublishedContent(HttpContext httpContext)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = PublishedContentFreshness,
            Extensions = { new NameValueHeaderValue(StaleWhileRevalidateDirective, StaleWhileRevalidateSeconds) }
        };
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
