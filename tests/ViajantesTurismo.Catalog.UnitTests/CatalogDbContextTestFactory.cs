using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Creates in-memory <see cref="CatalogDbContext"/> instances for tests.</summary>
internal static class CatalogDbContextTestFactory
{
    /// <summary>Creates a context over a new, uniquely named in-memory database.</summary>
    public static CatalogDbContext Create()
    {
        return Create(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot());
    }

    /// <summary>Creates a context over the named in-memory database within the shared root.</summary>
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
