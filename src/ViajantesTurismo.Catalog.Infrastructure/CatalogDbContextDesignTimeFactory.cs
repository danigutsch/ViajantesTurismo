using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContextDesignTimeFactory()
    {
    }

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<CatalogDbContext>();

        using var serviceProvider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Database=catalog-design-time")
            .Options;

        return new CatalogDbContext(
            options,
            serviceProvider.GetServices<IDbContextConfiguration<CatalogDbContext>>());
    }
}
