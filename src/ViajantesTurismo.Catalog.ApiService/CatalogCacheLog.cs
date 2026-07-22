namespace ViajantesTurismo.Catalog.ApiService;

internal static partial class CatalogCacheLog
{
    [LoggerMessage(
        EventId = 1,
        EventName = nameof(PublicCacheAreaInvalidated),
        Level = LogLevel.Information,
        Message = "Invalidated public cache area {CacheArea}.")]
    public static partial void PublicCacheAreaInvalidated(this ILogger logger, string cacheArea);

    [LoggerMessage(
        EventId = 2,
        EventName = nameof(TourProjectionPending),
        Level = LogLevel.Warning,
        Message = "A catalog tour was committed but its inline projection is pending. Failure type: {FailureType}.")]
    public static partial void TourProjectionPending(this ILogger logger, string failureType);

    [LoggerMessage(
        EventId = 3,
        EventName = nameof(PublicCacheAreaInvalidationFailed),
        Level = LogLevel.Warning,
        Message = "Could not invalidate public cache area {CacheArea}. Failure type: {FailureType}.")]
    public static partial void PublicCacheAreaInvalidationFailed(this ILogger logger, string cacheArea, string failureType);
}
