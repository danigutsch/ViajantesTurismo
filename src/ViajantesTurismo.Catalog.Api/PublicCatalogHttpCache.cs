using Microsoft.AspNetCore.OutputCaching;
using PandaBear.BuildingBlocks;
using PandaBear.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Catalog.Api;

/// <summary>Cache area, tag and header settings for public catalog responses.</summary>
internal static class PublicCatalogHttpCache
{
    /// <summary>Cache invalidation area name.</summary>
    public const string Area = "public-catalog";

    /// <summary>Cache tag applied to responses.</summary>
    public const string Tag = "public-catalog";

    /// <summary>Time public responses stay fresh.</summary>
    public static readonly TimeSpan Freshness = TimeSpan.FromSeconds(60);

    /// <summary>Sets public cache headers using the freshness window.</summary>
    public static void SetPublicHeaders(HttpContext httpContext)
    {
        HttpCacheHeaders.SetPublic(httpContext, Freshness);
    }

    /// <summary>Evicts cached public responses after a tour or media change.</summary>
    internal static async Task Invalidate(IOutputCacheStore outputCacheStore, ILogger logger)
    {
        try
        {
            await outputCacheStore.EvictByTagAsync(Tag, CancellationToken.None);
            logger.PublicCacheAreaInvalidated(Area);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(CancellationToken.None))
        {
            logger.PublicCacheAreaInvalidationFailed(Area, exception.GetType().Name);
        }
    }
}
