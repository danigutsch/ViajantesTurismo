using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Read-only metadata about one Admin outbox message.</summary>
public sealed record OutboxDeliveryRowDto
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

    /// <summary>Gets when the outbox enqueued the message.</summary>
    public required DateTimeOffset RecordedAt
    {
        get; init;
    }

    /// <summary>Gets the number of publication attempts.</summary>
    public required int Attempts
    {
        get; init;
    }

    /// <summary>Gets when the most recent publication attempt occurred.</summary>
    public required DateTimeOffset? LastAttemptAt
    {
        get; init;
    }

    /// <summary>Gets when the outbox has scheduled another publication attempt.</summary>
    public required DateTimeOffset? NextAttemptAt
    {
        get; init;
    }

    /// <summary>Gets when the outbox published the message, when retained publication metadata was requested.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? PublishedAt
    {
        get; init;
    }
}
