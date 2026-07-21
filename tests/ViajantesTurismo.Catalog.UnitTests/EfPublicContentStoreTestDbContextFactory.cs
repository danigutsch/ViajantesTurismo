using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Idempotency.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class EfPublicContentStoreTestDbContextFactory
{
    public static CatalogDbContext Create()
    {
        return Create(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot());
    }

    public static CatalogDbContext Create(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        return new CatalogDbContext(options, [new IdempotencyDbContextConfiguration<CatalogDbContext>()]);
    }
}
