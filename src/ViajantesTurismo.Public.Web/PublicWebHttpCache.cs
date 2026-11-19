using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>
/// HTTP cache headers of Public Web responses. Successful anonymous pages become public for
/// <see cref="PublishedContentFreshness" /> through <c>UseDefaultPublicCaching</c>; these helpers mark the exceptions.
/// </summary>
internal static class PublicWebHttpCache
{
    /// <summary>How long published content may be served from shared caches.</summary>
    public static readonly TimeSpan PublishedContentFreshness = TimeSpan.FromSeconds(60);

    /// <summary>Marks a visibility-dependent response as not cacheable.</summary>
    public static void SetNoStore(HttpContext? httpContext)
    {
        if (httpContext is { Response.HasStarted: false })
        {
            HttpCacheHeaders.SetNoStore(httpContext);
        }
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
}
