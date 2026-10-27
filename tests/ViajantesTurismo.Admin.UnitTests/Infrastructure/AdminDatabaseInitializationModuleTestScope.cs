using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.Domain;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Test scope over a built service provider exposing the database initialization services; disposing it disposes the scope and the provider.</summary>
internal sealed class AdminDatabaseInitializationModuleTestScope : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    /// <summary>Creates a scope on the provider and resolves the initializer, outbox, dispatcher and hosted services.</summary>
    public AdminDatabaseInitializationModuleTestScope(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        scope = serviceProvider.CreateScope();
        Initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        Outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        Dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        HostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
    }

    /// <summary>Gets the development data initializer resolved from the scope.</summary>
    public DevelopmentDataInitializer Initializer
    {
        get;
    }

    /// <summary>Gets the integration event outbox resolved from the scope.</summary>
    public IIntegrationEventOutbox Outbox
    {
        get;
    }

    /// <summary>Gets the domain event dispatcher resolved from the scope.</summary>
    public IDomainEventDispatcher Dispatcher
    {
        get;
    }

    /// <summary>Gets the hosted services registered in the provider.</summary>
    public IReadOnlyList<IHostedService> HostedServices
    {
        get;
    }

    /// <summary>Disposes the scope and the service provider.</summary>
    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
