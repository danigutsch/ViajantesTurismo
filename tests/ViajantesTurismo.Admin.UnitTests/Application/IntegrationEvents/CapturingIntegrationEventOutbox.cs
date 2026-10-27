using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

/// <summary>
/// Outbox fake that keeps the last enqueued integration event and whether it was enqueued before the unit of work saved.
/// </summary>
/// <param name="unitOfWork">The unit of work whose save count is inspected at enqueue time.</param>
internal sealed class CapturingIntegrationEventOutbox(FakeUnitOfWork unitOfWork) : IIntegrationEventOutbox, IDomainEventIntegrationEventOutbox
{
    /// <summary>The last enqueued integration event, or null when none was enqueued.</summary>
    public object? IntegrationEvent
    {
        get; private set;
    }

    /// <summary>True when the last event was enqueued before any save of the unit of work.</summary>
    public bool WasEnqueuedBeforeSave
    {
        get; private set;
    }

    /// <summary>Captures the event and records whether no save had happened yet.</summary>
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();

        IntegrationEvent = integrationEvent;
        WasEnqueuedBeforeSave = unitOfWork.SaveEntitiesCallCount == 0;

        return ValueTask.CompletedTask;
    }
}
