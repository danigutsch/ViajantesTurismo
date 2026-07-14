using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Host"] = "clamav",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Port"] = "3310"
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateDevelopmentScenario()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test"
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateOpenApiBuildGenerationScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Host"] = "clamav",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Port"] = "3310",
            ["OpenApi:BuildGeneration"] = bool.TrueString
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateConfiguredDevelopmentScenario()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Host"] = "clamav",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Port"] = "3310"
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateSeaweedFsScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Host"] = "clamav",
            [$"{ClamAvMediaUploadScannerOptions.SectionName}:Port"] = "3310",
            [$"{SeaweedFsMediaObjectStorageOptions.SectionName}:Endpoint"] = "https://seaweedfs.example",
            [$"{SeaweedFsMediaObjectStorageOptions.SectionName}:Bucket"] = "media",
            [$"{SeaweedFsMediaObjectStorageOptions.SectionName}:AccessKey"] = "access",
            [$"{SeaweedFsMediaObjectStorageOptions.SectionName}:SecretKey"] = "secret"
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateSeedingScenario()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes-catalog;Username=test;Password=test"
        });

        builder.AddCatalogSeeding();

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

    public void ShouldResolveSingleton<TService>()
        where TService : class
    {
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<TService>();
        var second = secondScope.ServiceProvider.GetRequiredService<TService>();

        ReferenceEquals(first, second).ShouldBeTrue();
    }

    public void ShouldResolveDbContextOptions<TContext>()
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        provider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<TContext>>().ShouldNotBeNull();
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
