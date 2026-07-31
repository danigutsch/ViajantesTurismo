namespace ViajantesTurismo.Branding.ApiService;

internal static partial class BrandingCacheLog
{
    [LoggerMessage(
        EventId = 1,
        EventName = nameof(PublicCacheAreaInvalidated),
        Level = LogLevel.Information,
        Message = "Invalidated public cache area {CacheArea}.")]
    public static partial void PublicCacheAreaInvalidated(this ILogger logger, string cacheArea);

    [LoggerMessage(
        EventId = 2,
        EventName = nameof(PublicCacheAreaInvalidationFailed),
        Level = LogLevel.Warning,
        Message = "Failed to invalidate public cache area {CacheArea}; failure type {FailureType}.")]
    public static partial void PublicCacheAreaInvalidationFailed(
        this ILogger logger,
        string cacheArea,
        string failureType);
}
