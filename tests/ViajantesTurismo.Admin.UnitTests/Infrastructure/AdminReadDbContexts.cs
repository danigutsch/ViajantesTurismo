using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminReadDbContexts
{
    public static AdminReadDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AdminReadDbContext(options);
    }
}
