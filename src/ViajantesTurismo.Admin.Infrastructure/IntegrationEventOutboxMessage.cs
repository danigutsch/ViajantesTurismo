namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class IntegrationEventOutboxMessage
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset EnqueuedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
}
