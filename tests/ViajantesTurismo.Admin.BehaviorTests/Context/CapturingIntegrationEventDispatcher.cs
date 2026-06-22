using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

public sealed class CapturingIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    public object? LastEvent { get; private set; }

    public ValueTask Dispatch<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        LastEvent = integrationEvent;

        return ValueTask.CompletedTask;
    }
}
