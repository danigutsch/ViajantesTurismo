using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.AuditTrail;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.Mediator;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Builds Admin infrastructure test scopes with different module compositions.</summary>
internal static class AdminInfrastructureModuleTestServices
{
    /// <summary>Composes the application only, with a capturing audit sink and no outbox.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithoutOutbox()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, CapturingDocumentAuditTrailSink>();
        builder.Services.AddSingleton<IAuditTrailSink<TourRetirementAuditRecord>, TourRetirementAuditTrailSink>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    /// <summary>Composes the write context with the integration event contract and outbox module.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithOutboxModule()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    /// <summary>Composes the write context with domain event dispatch, the integration event contract and the outbox.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithWriteContext()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    /// <summary>Composes only the outbox module around the given outbox instance.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithOutbox(IIntegrationEventOutbox outbox)
    {
        var services = new ServiceCollection();
        services.AddSingleton(outbox);
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(services.BuildServiceProvider());
    }

    /// <summary>Composes the application and the full infrastructure module for the given environment, validating the provider on build.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithInfrastructureModule(
        string environmentName = "Production",
        FakeTourStore? tourStore = null,
        ICustomerStore? customerStore = null)
    {
        var builder = CreateConfiguredApplicationBuilder(environmentName);
        if (tourStore is not null)
        {
            builder.Services.AddScoped<ITourStore>(_ => tourStore);
            builder.Services.AddScoped<ITourCommandStore>(_ => tourStore);
        }

        if (customerStore is not null)
        {
            builder.Services.AddScoped(_ => customerStore);
        }

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.Services.AddSharedKernelMediator();

        return CreateScope(BuildValidatedServiceProvider(builder.Services));
    }

    /// <summary>Composes the tour stores without the Admin write context.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithTourPersistenceOnly()
    {
        var services = new ServiceCollection();
        services.AddTourPersistence();

        return CreateScope(services.BuildServiceProvider());
    }

    /// <summary>Composes the application and infrastructure without runtime background services, as at OpenAPI build-time generation.</summary>
    public static AdminInfrastructureModuleTestScope CreateWithOpenApiBuildGenerationInfrastructureModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);
        builder.Services.AddSharedKernelMediator();

        return CreateScope(BuildValidatedServiceProvider(builder.Services));
    }

    /// <summary>Composes the development database initialization module and returns its scope.</summary>
    public static AdminDatabaseInitializationModuleTestScope CreateWithDatabaseInitializationModule()
    {
        var builder = CreateConfiguredApplicationBuilder(Environments.Development);
        builder.Services.AddDomainEventProcessing();
        builder.AddAdminDatabaseInitialization();

        var serviceProvider = BuildValidatedServiceProvider(builder.Services);
        try
        {
            return new AdminDatabaseInitializationModuleTestScope(serviceProvider);
        }
        catch
        {
            serviceProvider.Dispose();
            throw;
        }
    }

    private static HostApplicationBuilder CreateApplicationBuilderWithWriteContext()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, CapturingDocumentAuditTrailSink>();
        builder.Services.AddSingleton<IAuditTrailSink<TourRetirementAuditRecord>, TourRetirementAuditTrailSink>();
        builder.Services.AddDbContext<AdminWriteDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        return builder;
    }

    private static HostApplicationBuilder CreateConfiguredApplicationBuilder(string? environmentName = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test"
        });
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });

        return builder;
    }

    private static ServiceProvider BuildValidatedServiceProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

    private static AdminInfrastructureModuleTestScope CreateScope(ServiceProvider serviceProvider)
    {
        try
        {
            return new AdminInfrastructureModuleTestScope(serviceProvider);
        }
        catch
        {
            serviceProvider.Dispose();
            throw;
        }
    }
}
