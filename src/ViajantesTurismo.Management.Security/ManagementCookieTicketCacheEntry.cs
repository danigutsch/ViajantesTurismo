namespace ViajantesTurismo.Management.Security;

/// <summary>Persisted cache entry holding a serialized management authentication cookie ticket.</summary>
internal sealed class ManagementCookieTicketCacheEntry
{
    /// <summary>Cache key of the ticket.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Serialized authentication ticket bytes.</summary>
    public byte[] Value { get; init; } = [];

    /// <summary>Absolute time at which the entry expires.</summary>
    public DateTimeOffset ExpiresAtTime
    {
        get; init;
    }

    /// <summary>Sliding expiration window in seconds, or null when none applies.</summary>
    public long? SlidingExpirationInSeconds
    {
        get; init;
    }

    /// <summary>Absolute expiration time, or null when none applies.</summary>
    public DateTimeOffset? AbsoluteExpiration
    {
        get; init;
    }
}
