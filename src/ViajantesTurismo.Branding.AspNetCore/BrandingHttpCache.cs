using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>Defines caching behavior of the public Branding response.</summary>
internal static class BrandingHttpCache
{
    /// <summary>How long a public response stays fresh.</summary>
    public static readonly TimeSpan PublicFreshness = TimeSpan.FromSeconds(60);

    /// <summary>Output cache tag for public Branding responses.</summary>
    public const string PublicBrandingTag = "public-branding";

    /// <summary>Cache area name used in invalidation logs.</summary>
    public const string PublicBrandingArea = "public-branding";

    private static readonly TimeSpan StaleWhileRevalidate = TimeSpan.FromSeconds(300);

    /// <summary>Sets public cache headers on the response.</summary>
    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, PublicFreshness, StaleWhileRevalidate);
    }

}
