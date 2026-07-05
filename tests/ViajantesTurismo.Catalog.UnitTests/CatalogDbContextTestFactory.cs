using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogDbContextTestFactory
{
    public static CatalogDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CatalogDbContext(options, [new IdempotencyDbContextConfiguration<CatalogDbContext>()]);
    }
}
