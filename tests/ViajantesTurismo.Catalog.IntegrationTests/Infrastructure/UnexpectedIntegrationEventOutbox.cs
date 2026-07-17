using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

internal sealed class UnexpectedIntegrationEventOutbox : IIntegrationEventOutbox
{
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent => throw new InvalidOperationException("An outbox event is not expected when scanning fails.");
}
