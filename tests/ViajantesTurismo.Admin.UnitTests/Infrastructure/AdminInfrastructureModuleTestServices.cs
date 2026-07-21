using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.AuditTrail;
using SharedKernel.Branding;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.UnitTests.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminInfrastructureModuleTestServices
{
    public static AdminInfrastructureModuleTestScope CreateWithoutOutbox()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, CapturingDocumentAuditTrailSink>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    public static AdminInfrastructureModuleTestScope CreateWithOutboxModule()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    public static AdminInfrastructureModuleTestScope CreateWithWriteContext()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(builder.Services.BuildServiceProvider());
    }

    public static AdminInfrastructureModuleTestScope CreateWithOutbox(IIntegrationEventOutbox outbox)
    {
        var services = new ServiceCollection();
        services.AddSingleton(outbox);
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return CreateScope(services.BuildServiceProvider());
    }

    public static AdminInfrastructureModuleTestScope CreateWithInfrastructureModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.AddApplication();
        builder.AddInfrastructure();

        return CreateScope(BuildValidatedServiceProvider(builder.Services));
    }

    public static AdminInfrastructureModuleTestScope CreateWithOpenApiBuildGenerationInfrastructureModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.AddApplication();
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);

        return CreateScope(BuildValidatedServiceProvider(builder.Services));
    }

    public static AdminSeedingModuleTestScope CreateWithSeedingModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.Services.AddDomainEventProcessing();
        builder.AddAdminSeeding();

        var serviceProvider = BuildValidatedServiceProvider(builder.Services);
        try
        {
            return new AdminSeedingModuleTestScope(serviceProvider);
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
        builder.Services.AddDbContext<AdminWriteDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        return builder;
    }

    private static HostApplicationBuilder CreateConfiguredApplicationBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IBrandingApiClient>(
            new FakeBrandingApiClient(new BrandingSettingsDto()));
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test"
        });

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
