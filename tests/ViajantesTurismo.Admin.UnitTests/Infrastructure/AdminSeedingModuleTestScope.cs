using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.DomainEvents;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminSeedingModuleTestScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    public AdminSeedingModuleTestScope(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        scope = serviceProvider.CreateScope();
        Seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
        Outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        Dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        HostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
    }

    public Seeder Seeder { get; }

    public IIntegrationEventOutbox Outbox { get; }

    public IDomainEventDispatcher Dispatcher { get; }

    public IReadOnlyList<IHostedService> HostedServices { get; }

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
