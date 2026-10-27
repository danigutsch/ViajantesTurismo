using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Creates <see cref="CatalogDbContext" /> for EF Core design-time tooling such as migrations.</summary>
internal sealed class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    /// <summary>Initializes a new instance of the <see cref="CatalogDbContextDesignTimeFactory" /> class.</summary>
    public CatalogDbContextDesignTimeFactory()
    {
    }

    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<CatalogDbContext>();
        services.AddIntegrationEventInbox<CatalogDbContext>();
        services.AddIntegrationEventTransportStorage<CatalogDbContext>();

        using var serviceProvider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalog-design-time")
            .Options;

        return new CatalogDbContext(
            options,
            serviceProvider.GetServices<IDbContextConfiguration<CatalogDbContext>>());
    }
}
