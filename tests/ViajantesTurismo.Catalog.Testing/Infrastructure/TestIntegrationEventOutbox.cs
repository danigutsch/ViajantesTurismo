using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Outbox that discards enqueued integration events.</summary>
internal sealed class TestIntegrationEventOutbox : IIntegrationEventOutbox
{
    /// <summary>Discards the event; throws only if cancellation was requested.</summary>
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
