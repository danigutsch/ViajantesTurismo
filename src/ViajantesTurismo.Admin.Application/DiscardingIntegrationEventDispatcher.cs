using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Application;

internal sealed class DiscardingIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    public ValueTask Dispatch<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return ValueTask.CompletedTask;
    }
}
