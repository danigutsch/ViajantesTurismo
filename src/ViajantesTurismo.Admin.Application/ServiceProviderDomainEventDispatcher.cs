using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.DomainEvents;

namespace ViajantesTurismo.Admin.Application;

internal sealed class ServiceProviderDomainEventDispatcher(
    IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();
        foreach (var handler in handlers)
        {
            await handler.Handle(domainEvent, ct);
        }
    }
}
