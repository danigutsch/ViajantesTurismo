using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Creates Admin read contexts for tests.</summary>
internal static class AdminReadDbContexts
{
    /// <summary>Creates an Admin read context over a new uniquely named in-memory database.</summary>
    public static AdminReadDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AdminReadDbContext(options);
    }
}
