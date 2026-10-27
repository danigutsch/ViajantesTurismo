using Microsoft.EntityFrameworkCore.Storage;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Creates in-memory <c>CatalogDbContext</c> instances for public content store tests.</summary>
internal static class EfPublicContentStoreTestDbContextFactory
{
    /// <summary>Creates a context over a new, uniquely named in-memory database.</summary>
    public static CatalogDbContext Create()
    {
        return Create(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot());
    }

    /// <summary>Creates a context over the named in-memory database, sharing state through the given root.</summary>
    public static CatalogDbContext Create(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        return CatalogDbContextTestFactory.Create(databaseName, databaseRoot);
    }
}
