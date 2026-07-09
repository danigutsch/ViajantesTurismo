namespace ViajantesTurismo.Catalog.ApiService;

internal static partial class CatalogCacheLog
{
    [LoggerMessage(
        EventId = 1,
        EventName = nameof(PublicCatalogCacheInvalidated),
        Level = LogLevel.Information,
        Message = "Invalidated public catalog cache area {CacheArea}.")]
    public static partial void PublicCatalogCacheInvalidated(this ILogger logger, string cacheArea);
}
