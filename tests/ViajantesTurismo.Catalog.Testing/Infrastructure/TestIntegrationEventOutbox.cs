using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal sealed class TestIntegrationEventOutbox : IIntegrationEventOutbox
{
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
