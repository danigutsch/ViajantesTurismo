using PandaBear.Messaging;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Records event types delivered to the local Catalog dispatcher.</summary>
internal sealed class CapturingCatalogOutboxPublisher : IEventEnvelopePublisher
{
    private readonly List<string> _eventTypes = [];

    /// <summary>Gets the event types received by the dispatcher.</summary>
    internal IReadOnlyList<string> EventTypes => _eventTypes;

    /// <inheritdoc />
    public ValueTask Publish(EventEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ct.ThrowIfCancellationRequested();
        _eventTypes.Add(envelope.EventType);
        return ValueTask.CompletedTask;
    }
}
