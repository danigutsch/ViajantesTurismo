using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Domain;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminDatabaseInitializationModuleTestScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    public AdminDatabaseInitializationModuleTestScope(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        scope = serviceProvider.CreateScope();
        Initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        Outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        Dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        HostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
    }

    public DevelopmentDataInitializer Initializer { get; }

    public IIntegrationEventOutbox Outbox { get; }

    public IDomainEventDispatcher Dispatcher { get; }

    public IReadOnlyList<IHostedService> HostedServices { get; }

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
