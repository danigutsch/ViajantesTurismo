using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

/// <summary>Outbox fake that fails the test when an event is enqueued.</summary>
internal sealed class UnexpectedIntegrationEventOutbox : IIntegrationEventOutbox
{
    /// <summary>Always throws because no outbox event is expected when scanning fails.</summary>
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent => throw new InvalidOperationException("An outbox event is not expected when scanning fails.");
}
