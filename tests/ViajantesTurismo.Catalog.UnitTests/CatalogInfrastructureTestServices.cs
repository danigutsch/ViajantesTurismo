using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogInfrastructureTestServices
{
    public static CatalogInfrastructureScenario CreateScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test"
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateWorkerScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes-catalog;Username=test;Password=test",
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test"
        });

        builder.AddCatalogIntegrationEventWorkerInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateApiHostedTransportScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test"
        });

        builder.AddCatalogHostedIntegrationEventTransport();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }
}

internal sealed class CatalogInfrastructureScenario(ServiceProvider provider) : IDisposable
{
    private readonly IHostedService[] hostedServices = provider.GetServices<IHostedService>().ToArray();

    public void ShouldIncludeHostedService<TService>()
    {
        ContainsHostedService<TService>().ShouldBeTrue();
    }

    public void ShouldNotIncludeHostedService<TService>()
    {
        ContainsHostedService<TService>().ShouldBeFalse();
    }

    public bool ContainsHostedService<TService>()
    {
        return hostedServices.Any(service => service.GetType() == typeof(TService));
    }

    public void ShouldResolve<TService>()
        where TService : class
    {
        provider.GetRequiredService<TService>().ShouldNotBeNull();
    }

    public void ShouldResolveAs<TService, TImplementation>()
        where TService : notnull
    {
        provider.GetRequiredService<TService>().ShouldBeOfType<TImplementation>();
    }

    public void ShouldResolveEnumerableItemAs<TService, TImplementation>()
        where TService : notnull
    {
        var service = provider.GetServices<TService>().ShouldHaveSingleItem(item => item?.GetType() == typeof(TImplementation));
        service.ShouldBeOfType<TImplementation>();
    }

    public void Dispose()
    {
        provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
