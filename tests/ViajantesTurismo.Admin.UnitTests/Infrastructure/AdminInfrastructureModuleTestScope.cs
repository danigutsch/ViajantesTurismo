using ViajantesTurismo.Admin.Application.Branding;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PandaBear.Domain;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Test scope over a built service provider exposing the Admin infrastructure services; disposing it disposes the scope and the provider.</summary>
/// <param name="serviceProvider">The built provider that owns the scope and is disposed with it.</param>
internal sealed class AdminInfrastructureModuleTestScope(ServiceProvider serviceProvider) : IDisposable
{
    private readonly ServiceProvider serviceProvider = serviceProvider;
    private readonly IServiceScope scope = serviceProvider.CreateScope();

    /// <summary>Gets whether the outbox relay hosted service for the Admin write context is registered.</summary>
    public bool HasOutboxRelay => HostedServices.Any(service =>
    {
        var type = service.GetType();
        return type.Assembly == typeof(PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore.IntegrationEventOutboxRelayOptions).Assembly
            && type.IsGenericType
            && type.GetGenericTypeDefinition().Name == "IntegrationEventOutboxRelayHostedService`1"
            && type.GenericTypeArguments.Single() == typeof(AdminWriteDbContext);
    });

    /// <summary>Gets the domain event dispatcher resolved from the scope.</summary>
    public IDomainEventDispatcher Dispatcher => scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

    /// <summary>Gets the Admin write context resolved from the scope.</summary>
    public AdminWriteDbContext WriteContext => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

    /// <summary>Gets the unit of work resolved from the scope.</summary>
    public IUnitOfWork UnitOfWork => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    /// <summary>Gets the query service resolved from the scope.</summary>
    public IQueryService QueryService => scope.ServiceProvider.GetRequiredService<IQueryService>();

    /// <summary>Gets the tour store resolved from the scope.</summary>
    public ITourStore TourStore => scope.ServiceProvider.GetRequiredService<ITourStore>();

    /// <summary>Gets the tour capacity mutation lock resolved from the scope.</summary>
    public ITourCapacityMutationLock TourCapacityMutationLock =>
        scope.ServiceProvider.GetRequiredService<ITourCapacityMutationLock>();

    /// <summary>Gets the customer store resolved from the scope.</summary>
    public ICustomerStore CustomerStore => scope.ServiceProvider.GetRequiredService<ICustomerStore>();

    /// <summary>Gets the document store resolved from the scope.</summary>
    public IDocumentStore DocumentStore => scope.ServiceProvider.GetRequiredService<IDocumentStore>();

    /// <summary>Gets the integration event outbox resolved from the scope.</summary>
    public IIntegrationEventOutbox Outbox => scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

    /// <summary>Gets the branding projection store resolved from the scope.</summary>
    public IBrandingProjectionStore BrandingProjectionStore => scope.ServiceProvider.GetRequiredService<IBrandingProjectionStore>();

    /// <summary>Gets the idempotency store, or null when none is registered.</summary>
    public IIdempotencyStore? IdempotencyStore => scope.ServiceProvider.GetService<IIdempotencyStore>();

    /// <summary>Gets the DbContext option configurations registered for the Admin write context.</summary>
    public IReadOnlyList<IDbContextConfiguration<AdminWriteDbContext>> DbContextConfigurations =>
        [.. scope.ServiceProvider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>()];

    /// <summary>Gets the hosted services registered in the provider.</summary>
    public IReadOnlyList<IHostedService> HostedServices => [.. serviceProvider.GetServices<IHostedService>()];

    /// <summary>Returns whether the tour store is the same instance within a scope and a different instance across scopes.</summary>
    public bool TourStoreHasScopedLifetime()
    {
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();
        var firstStore = firstScope.ServiceProvider.GetRequiredService<ITourStore>();
        var sameScopeStore = firstScope.ServiceProvider.GetRequiredService<ITourStore>();
        var secondStore = secondScope.ServiceProvider.GetRequiredService<ITourStore>();

        return ReferenceEquals(firstStore, sameScopeStore) && !ReferenceEquals(firstStore, secondStore);
    }

    /// <summary>Returns whether sensitive data logging is enabled in the options of the given context type.</summary>
    public bool IsSensitiveDataLoggingEnabled<TContext>() where TContext : DbContext
    {
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<TContext>>();
        return options.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }

    /// <summary>Returns whether starting an activity on the named source is sampled once the tracer provider is built.</summary>
    public bool IsActivitySourceEnabled(string sourceName)
    {
        _ = serviceProvider.GetRequiredService<TracerProvider>();
        using var activitySource = new ActivitySource(sourceName);
        using var activity = activitySource.StartActivity("privacy-test", ActivityKind.Client);
        return activity is not null;
    }

    /// <summary>Returns whether a meter provider is registered.</summary>
    public bool HasMeterProvider()
    {
        return serviceProvider.GetService<MeterProvider>() is not null;
    }

    /// <summary>Disposes the scope and the service provider.</summary>
    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
