using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogInfrastructureTestServices
{
    public static void AddProductionInfrastructureWithoutMediaStorage()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310"
        });

        builder.AddCatalogInfrastructure();
    }

    public static CatalogInfrastructureScenario CreateScenario()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310"
        });
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });

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
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateOpenApiBuildGenerationScenario()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310"
        });

        builder.AddCatalogInfrastructure(addOutboxRelay: false);

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
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310"
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
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310",
            [$"{MediaObjectStorageConfiguration.SectionName}:Endpoint"] = "https://seaweedfs.example",
            [$"{MediaObjectStorageConfiguration.SectionName}:Bucket"] = "media",
            [$"{MediaObjectStorageConfiguration.SectionName}:AccessKey"] = "access",
            [$"{MediaObjectStorageConfiguration.SectionName}:SecretKey"] = "secret"
        });
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });

        builder.AddCatalogInfrastructure();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateS3Scenario(bool includeSeaweedFsSection)
    {
        var builder = Host.CreateApplicationBuilder();
        var configuration = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:Endpoint"] = "https://s3.eu-central-1.amazonaws.com",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:AuthenticationRegion"] = "eu-central-1",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:ForcePathStyle"] = "false",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:Bucket"] = "cloud-media",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:AccessKey"] = "test-access",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:SecretKey"] = "test-secret",
            [$"{MediaObjectStorageConfiguration.S3SectionName}:PublicBaseUri"] = "https://cdn.example.com/published/"
        };
        if (includeSeaweedFsSection)
        {
            // An incomplete local section must not override valid production configuration.
            configuration[$"{MediaObjectStorageConfiguration.SectionName}:Endpoint"] = "http://seaweedfs:8333";
        }
        builder.Configuration.AddInMemoryCollection(configuration);
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

        builder.AddCatalogDatabaseInitialization();

        return new CatalogInfrastructureScenario(builder.Services.BuildServiceProvider());
    }

    public static CatalogInfrastructureScenario CreateWorkerScenario(string environmentName = "Production")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        var configuration = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes-catalog;Username=test;Password=test",
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "clamav",
            [ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey] = "3310"
        };
        if (!builder.Environment.IsDevelopment())
        {
            configuration[$"{MediaObjectStorageConfiguration.SectionName}:Endpoint"] = "https://seaweedfs.example";
            configuration[$"{MediaObjectStorageConfiguration.SectionName}:Bucket"] = "media";
            configuration[$"{MediaObjectStorageConfiguration.SectionName}:AccessKey"] = "access";
            configuration[$"{MediaObjectStorageConfiguration.SectionName}:SecretKey"] = "secret";
        }
        builder.Configuration.AddInMemoryCollection(configuration);

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

    public static CatalogInfrastructureScenario CreateBrandingCoHostedScenario()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=viajantes;Username=test;Password=test",
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });

        // Branding registers first on purpose: the co-hosted composition must not depend on order.
        builder.AddBrandingInfrastructure();
        builder.AddCatalogInfrastructure();

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

    public TService ShouldResolve<TService>()
        where TService : class
    {
        var service = provider.GetRequiredService<TService>();
        service.ShouldNotBeNull();
        return service;
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
        where TContext : DbContext
    {
        provider.GetRequiredService<DbContextOptions<TContext>>().ShouldNotBeNull();
    }

    public bool IsSensitiveDataLoggingEnabled<TContext>()
        where TContext : DbContext
    {
        var options = provider.GetRequiredService<DbContextOptions<TContext>>();
        return options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
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

    public bool DefaultOutboxBelongsTo<TContext>()
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var defaultOutbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var contextOutbox = scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventOutbox>(typeof(TContext));
        return ReferenceEquals(defaultOutbox, contextOutbox);
    }

    public (string? Schema, string? Table) OutboxTable<TContext>()
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var entityType = scope.ServiceProvider.GetRequiredService<TContext>().Model.GetEntityTypes()
            .ShouldHaveSingleItem(entity => entity.ClrType.Name == "IntegrationEventOutboxMessage");
        return (entityType.GetSchema(), entityType.GetTableName());
    }

    public void Dispose()
    {
        provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
