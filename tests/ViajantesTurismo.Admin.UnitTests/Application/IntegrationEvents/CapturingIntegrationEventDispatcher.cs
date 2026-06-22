using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

internal sealed class CapturingIntegrationEventDispatcher(FakeUnitOfWork unitOfWork) : IIntegrationEventDispatcher
{
    public object? IntegrationEvent { get; private set; }

    public bool WasDispatchedAfterSave { get; private set; }

    public ValueTask Dispatch<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        IntegrationEvent = integrationEvent;
        WasDispatchedAfterSave = unitOfWork.SaveEntitiesCallCount == 1;

        return ValueTask.CompletedTask;
    }
}
