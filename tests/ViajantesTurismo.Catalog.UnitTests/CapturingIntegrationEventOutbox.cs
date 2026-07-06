using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class CapturingIntegrationEventOutbox : IIntegrationEventOutbox
{
    public object? IntegrationEvent { get; private set; }

    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();
        IntegrationEvent = integrationEvent;

        return ValueTask.CompletedTask;
    }
}
