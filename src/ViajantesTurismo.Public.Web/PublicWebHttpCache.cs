using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebHttpCache
{
    public static readonly TimeSpan PublishedContentFreshness = TimeSpan.FromSeconds(60);

    public const string CultureQueryKey = "culture";

    public const string LanguageQueryKey = "language";

    private static readonly TimeSpan StaleWhileRevalidate = TimeSpan.FromSeconds(300);

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

    public static void SetNoStore(HttpContext httpContext)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
    }

    public static void SetServiceUnavailableNoStore(HttpContext? httpContext)
    {
        if (httpContext is null || httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        SetNoStore(httpContext);
    }

    private static void SetPublishedContent(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, PublishedContentFreshness, StaleWhileRevalidate);
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
        var hasCultureInput = httpContext.Request.Query.ContainsKey(CultureQueryKey)
            || httpContext.Request.Query.ContainsKey(LanguageQueryKey);
        if (!hasCultureInput)
        {
            return;
        }

        var canonicalCulture = httpContext.Request.Query.TryGetValue(CultureQueryKey, out var cultureValue)
            ? PublicCultureQuery.NormalizeCulture(cultureValue.ToString())
            : null;

        if (canonicalCulture is null
            && httpContext.Request.Query.TryGetValue(LanguageQueryKey, out var language))
        {
            canonicalCulture = PublicCultureQuery.NormalizeCulture(language.ToString());
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

        if (canonicalCulture is not null)
        {
            queryValues.Add(new KeyValuePair<string, string?>(CultureQueryKey, canonicalCulture));
        }

        httpContext.Request.QueryString = QueryString.Create(queryValues);
    }
}
