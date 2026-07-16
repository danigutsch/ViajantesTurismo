using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class PublicCatalogHttpCache
{
    public const string Area = "public-catalog";

    public const string Tag = "public-catalog";

    public static readonly TimeSpan Freshness = TimeSpan.FromSeconds(60);

    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, Freshness);
    }
}
