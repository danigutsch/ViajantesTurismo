namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Reports aggregate delivery health for Admin-owned messaging.</summary>
public sealed record GetMessagingHealthDto
{
    /// <summary>Gets the UTC cutoff used for ages and due retries.</summary>
    public required DateTimeOffset ObservedAtUtc
    {
        get; init;
    }

    /// <summary>Gets the unpublished Admin outbox count when this producer reports outbox health.</summary>
    public long? AdminPendingOutboxCount
    {
        get;
        init;
    }

    /// <summary>Gets the oldest enqueue time among unpublished Admin outbox messages, when present.</summary>
    public DateTimeOffset? AdminOldestPendingOutboxEnqueuedAt
    {
        get;
        init;
    }

    /// <summary>Gets the number of Admin transport messages that exhausted delivery attempts to Catalog.</summary>
    public required long CatalogTerminalDeliveryFailureCount
    {
        get; init;
    }

    /// <summary>Gets the earliest terminal failure time for Catalog deliveries.</summary>
    public required DateTimeOffset? CatalogOldestTerminalFailureAt
    {
        get; init;
    }

    /// <summary>Gets the number of Catalog deliveries with a scheduled consume retry.</summary>
    public required long CatalogRetryCount
    {
        get; init;
    }

    /// <summary>Gets the earliest transport receipt time among scheduled Catalog retries.</summary>
    public required DateTimeOffset? CatalogOldestRetryReceivedAt
    {
        get; init;
    }

    /// <summary>Gets the number of scheduled Catalog retries due at the snapshot cutoff.</summary>
    public required long CatalogDueRetryCount
    {
        get; init;
    }
}
