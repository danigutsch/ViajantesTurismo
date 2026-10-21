using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogDbContextTestFactory
{
    public static CatalogDbContext Create()
    {
        return Create(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot());
    }

    public static CatalogDbContext Create(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        var services = new ServiceCollection();
        services.AddIdempotencyStore<CatalogDbContext>();
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<CatalogDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        return new CatalogDbContext(options, configurations);
    }
}
