namespace ViajantesTurismo.Management.Security;

internal sealed class ManagementCookieTicketCacheEntry
{
    public string Id { get; init; } = string.Empty;

    public byte[] Value { get; init; } = [];

    public DateTimeOffset ExpiresAtTime { get; init; }

    public long? SlidingExpirationInSeconds { get; init; }

    public DateTimeOffset? AbsoluteExpiration { get; init; }
}
