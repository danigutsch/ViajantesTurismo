namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Read-only metadata about one unfinished Admin-to-Catalog delivery.</summary>
public sealed record CatalogDeliveryRowDto
{
    /// <summary>Gets the opaque identifier used to order rows with equal timestamps.</summary>
    public required Guid StorageId
    {
        get; init;
    }

    /// <summary>Gets the event identifier assigned by the producer.</summary>
    public required string EventId
    {
        get; init;
    }

    /// <summary>Gets the registered event type.</summary>
    public required string EventType
    {
        get; init;
    }

    /// <summary>Gets when the transport recorded the delivery.</summary>
    public required DateTimeOffset RecordedAt
    {
        get; init;
    }

    /// <summary>Gets the number of delivery attempts.</summary>
    public required int Attempts
    {
        get; init;
    }

    /// <summary>Gets when the most recent attempt occurred.</summary>
    public required DateTimeOffset? LastAttemptAt
    {
        get; init;
    }

    /// <summary>Gets when the provider has scheduled another attempt.</summary>
    public required DateTimeOffset? NextAttemptAt
    {
        get; init;
    }

    /// <summary>Gets when the provider marked the delivery as a terminal failure.</summary>
    public required DateTimeOffset? TerminalAt
    {
        get; init;
    }
}
