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

/// <summary>Builds Catalog infrastructure service providers for composition tests.</summary>
internal static class CatalogInfrastructureTestServices
{
    /// <summary>Registers Catalog infrastructure in Production without media storage configuration.</summary>
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

    /// <summary>Creates a Development scenario with ClamAV configured.</summary>
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

    /// <summary>Creates a Development scenario with malware scanning disabled.</summary>
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

    /// <summary>Creates a scenario with the outbox relay disabled, as during build-time OpenAPI generation.</summary>
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

    /// <summary>Creates a Development scenario with ClamAV host and port configured.</summary>
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

    /// <summary>Creates a scenario configured with the SeaweedFS media storage section.</summary>
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

    /// <summary>Creates a scenario configured with S3 media storage, optionally with a conflicting incomplete SeaweedFS section.</summary>
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

    /// <summary>Creates a scenario that registers only Catalog database initialization.</summary>
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

    /// <summary>Creates a scenario for the integration event worker host; non-Development adds SeaweedFS storage settings.</summary>
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

    /// <summary>Creates a scenario for the API-hosted integration event transport.</summary>
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

    /// <summary>Creates a scenario registering Branding before Catalog infrastructure in one host.</summary>
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
