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

    /// <summary>How long a content-addressed response stays fresh: one year, the practical maximum.</summary>
    public static readonly TimeSpan ImmutableFreshness = TimeSpan.FromDays(365);

    /// <summary>Sets public cache headers on the response.</summary>
    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, PublicFreshness, StaleWhileRevalidate);
    }

    /// <summary>Sets headers for bytes whose address is their content: shared caches and browsers never revalidate.</summary>
    public static void SetImmutableHeaders(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Headers.CacheControl = $"public, max-age={(int)ImmutableFreshness.TotalSeconds}, immutable";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
    }

}
