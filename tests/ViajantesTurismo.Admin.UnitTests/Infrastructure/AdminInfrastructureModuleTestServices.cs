using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminInfrastructureModuleTestServices
{
    public static ServiceProvider CreateWithoutOutbox()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithOutboxModule()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithWriteContext()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();
        builder.Services.AddAdminIntegrationEventContract();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithOutbox(IIntegrationEventOutbox outbox)
    {
        var services = new ServiceCollection();
        services.AddSingleton(outbox);
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();

        return services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithInfrastructureModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.AddApplication();
        builder.AddInfrastructure();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithSeedingModule()
    {
        var builder = CreateConfiguredApplicationBuilder();
        builder.AddSeeding();

        return builder.Services.BuildServiceProvider();
    }

    private static HostApplicationBuilder CreateApplicationBuilderWithWriteContext()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddDbContext<AdminWriteDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        return builder;
    }

    private static HostApplicationBuilder CreateConfiguredApplicationBuilder()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=viajantes-admin;Username=test;Password=test"
        });

        return builder;
    }
}
