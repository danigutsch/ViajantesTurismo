using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Integration event outbox that keeps the last enqueued event for assertions.</summary>
internal sealed class CapturingIntegrationEventOutbox : IIntegrationEventOutbox
{
    /// <summary>Gets the last enqueued event, or null when nothing was enqueued.</summary>
    public object? IntegrationEvent
    {
        get; private set;
    }

    /// <summary>Captures the event instead of persisting it.</summary>
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();
        IntegrationEvent = integrationEvent;

        return ValueTask.CompletedTask;
    }
}
