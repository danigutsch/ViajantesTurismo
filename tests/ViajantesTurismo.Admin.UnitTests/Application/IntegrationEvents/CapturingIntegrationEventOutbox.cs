using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

internal sealed class CapturingIntegrationEventOutbox(FakeUnitOfWork unitOfWork) : IIntegrationEventOutbox, IDomainEventIntegrationEventOutbox
{
    public object? IntegrationEvent { get; private set; }

    public bool WasEnqueuedBeforeSave { get; private set; }

    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ct.ThrowIfCancellationRequested();

        IntegrationEvent = integrationEvent;
        WasEnqueuedBeforeSave = unitOfWork.SaveEntitiesCallCount == 0;

        return ValueTask.CompletedTask;
    }
}
