using Microsoft.Extensions.DependencyInjection;
using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Application;

internal sealed class ServiceProviderIntegrationEventDispatcher(
    IServiceProvider serviceProvider) : IIntegrationEventDispatcher
{
    public async ValueTask Dispatch<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>();
        foreach (var handler in handlers)
        {
            await handler.Handle(integrationEvent, ct);
        }
    }
}
