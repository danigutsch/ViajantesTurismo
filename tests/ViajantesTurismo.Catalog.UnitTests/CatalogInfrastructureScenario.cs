using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Built Catalog infrastructure service provider with helpers to assert registrations.</summary>
/// <param name="provider">Service provider under test, disposed with the scenario.</param>
internal sealed class CatalogInfrastructureScenario(ServiceProvider provider) : IDisposable
{
    private readonly IHostedService[] hostedServices = provider.GetServices<IHostedService>().ToArray();

    /// <summary>Asserts the hosted service type is registered.</summary>
    public void ShouldIncludeHostedService<TService>()
    {
        ContainsHostedService<TService>().ShouldBeTrue();
    }

    /// <summary>Asserts the hosted service type is not registered.</summary>
    public void ShouldNotIncludeHostedService<TService>()
    {
        ContainsHostedService<TService>().ShouldBeFalse();
    }

    /// <summary>Returns whether a hosted service of exactly the given type is registered.</summary>
    public bool ContainsHostedService<TService>()
    {
        return hostedServices.Any(service => service.GetType() == typeof(TService));
    }

    /// <summary>Returns whether a hosted service is the named outbox relay generic type over the given context.</summary>
    public bool ContainsHostedService<TContext>(string genericTypeName)
    {
        return hostedServices.Any(service =>
        {
            var type = service.GetType();
            return type.Assembly == typeof(PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore.IntegrationEventOutboxRelayOptions).Assembly
                && type.IsGenericType
                && type.GetGenericTypeDefinition().Name == genericTypeName
                && type.GenericTypeArguments.Single() == typeof(TContext);
        });
    }

    /// <summary>Resolves the required service and asserts it is not null.</summary>
    public TService ShouldResolve<TService>()
        where TService : class
    {
        var service = provider.GetRequiredService<TService>();
        service.ShouldNotBeNull();
        return service;
    }

    /// <summary>Asserts the service resolves to the same instance across scopes.</summary>
    public void ShouldResolveSingleton<TService>()
        where TService : class
    {
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<TService>();
        var second = secondScope.ServiceProvider.GetRequiredService<TService>();

        ReferenceEquals(first, second).ShouldBeTrue();
    }

    /// <summary>Asserts options for the context are registered.</summary>
    public void ShouldResolveDbContextOptions<TContext>()
        where TContext : DbContext
    {
        provider.GetRequiredService<DbContextOptions<TContext>>().ShouldNotBeNull();
    }

    /// <summary>Returns whether sensitive data logging is enabled on the context options.</summary>
    public bool IsSensitiveDataLoggingEnabled<TContext>()
        where TContext : DbContext
    {
        var options = provider.GetRequiredService<DbContextOptions<TContext>>();
        return options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }

    /// <summary>Asserts the service resolves to the implementation type.</summary>
    public void ShouldResolveAs<TService, TImplementation>()
        where TService : notnull
    {
        provider.GetRequiredService<TService>().ShouldBeOfType<TImplementation>();
    }

    /// <summary>Asserts exactly one registered item of the service is of the implementation type.</summary>
    public void ShouldResolveEnumerableItemAs<TService, TImplementation>()
        where TService : notnull
    {
        var service = provider.GetServices<TService>().ShouldHaveSingleItem(item => item?.GetType() == typeof(TImplementation));
        service.ShouldBeOfType<TImplementation>();
    }

    /// <summary>Returns whether the default outbox is the one keyed to the context.</summary>
    public bool DefaultOutboxBelongsTo<TContext>()
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var defaultOutbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var contextOutbox = scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventOutbox>(typeof(TContext));
        return ReferenceEquals(defaultOutbox, contextOutbox);
    }

    /// <summary>Returns the schema and table name of the context outbox message entity.</summary>
    public (string? Schema, string? Table) OutboxTable<TContext>()
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var entityType = scope.ServiceProvider.GetRequiredService<TContext>().Model.GetEntityTypes()
            .ShouldHaveSingleItem(entity => entity.ClrType.Name == "IntegrationEventOutboxMessage");
        return (entityType.GetSchema(), entityType.GetTableName());
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public void Dispose()
    {
        provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
