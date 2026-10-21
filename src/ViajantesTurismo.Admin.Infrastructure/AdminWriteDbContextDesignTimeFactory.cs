using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class AdminWriteDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AdminWriteDbContext>
{
    public AdminWriteDbContextDesignTimeFactory()
    {
    }

    public AdminWriteDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);

        using var serviceProvider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql("Host=localhost;Database=admin-design-time")
            .Options;

        return new AdminWriteDbContext(
            options,
            serviceProvider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>());
    }
}
