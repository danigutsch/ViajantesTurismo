using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminWriteDbContextTestFactory
{
    public static AdminWriteDbContext Create(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AdminWriteDbContext(options, [new TestInterceptorDbContextModule(interceptors)]);
    }
}
