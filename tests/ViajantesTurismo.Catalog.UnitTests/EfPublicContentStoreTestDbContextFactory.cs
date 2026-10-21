using Microsoft.EntityFrameworkCore.Storage;
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
        return CatalogDbContextTestFactory.Create(databaseName, databaseRoot);
    }
}
