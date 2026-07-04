using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Stores integration events with the current unit-of-work commit.
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>
    /// Adds an integration event to the durable outbox.
    /// </summary>
    /// <param name="integrationEvent">The integration event to store.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <typeparam name="TIntegrationEvent">The integration event type.</typeparam>
    /// <returns>A task that completes when the event is tracked for persistence.</returns>
    ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent;
}
