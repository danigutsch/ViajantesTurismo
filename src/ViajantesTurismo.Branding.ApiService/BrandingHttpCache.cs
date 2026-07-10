using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Branding.ApiService;

internal static class BrandingHttpCache
{
    public static readonly TimeSpan PublicFreshness = TimeSpan.FromSeconds(60);

    public const string PublicBrandingTag = "public-branding";

    public const string PublicBrandingArea = "public-branding";

    private static readonly TimeSpan StaleWhileRevalidate = TimeSpan.FromSeconds(300);

    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, PublicFreshness, StaleWhileRevalidate);
    }

    public static void SetNoStore(HttpContext httpContext)
    {
        HttpCacheHeaders.SetNoStore(httpContext);
    }
}
