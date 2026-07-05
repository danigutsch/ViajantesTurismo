using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;

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
        builder.Services.AddIntegrationEventOutboxModule();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithWriteContext()
    {
        var builder = CreateApplicationBuilderWithWriteContext();
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();
        builder.Services.AddIntegrationEventOutboxModule();

        return builder.Services.BuildServiceProvider();
    }

    public static ServiceProvider CreateWithOutbox(IIntegrationEventOutbox outbox)
    {
        var services = new ServiceCollection();
        services.AddSingleton(outbox);
        services.AddIntegrationEventOutboxModule();

        return services.BuildServiceProvider();
    }

    private static HostApplicationBuilder CreateApplicationBuilderWithWriteContext()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddApplication();
        builder.Services.AddDbContext<AdminWriteDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        return builder;
    }
}
