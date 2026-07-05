using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class EfPublicContentStoreTestDbContextFactory
{
    public static CatalogDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options, [new IdempotencyDbContextConfiguration<CatalogDbContext>()]);
    }
}
