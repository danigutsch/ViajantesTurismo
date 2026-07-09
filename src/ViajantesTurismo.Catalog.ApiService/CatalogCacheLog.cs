namespace ViajantesTurismo.Catalog.ApiService;

internal static partial class CatalogCacheLog
{
    [LoggerMessage(
        EventId = 1,
        EventName = nameof(PublicCacheAreaInvalidated),
        Level = LogLevel.Information,
        Message = "Invalidated public cache area {CacheArea}.")]
    public static partial void PublicCacheAreaInvalidated(this ILogger logger, string cacheArea);
}
