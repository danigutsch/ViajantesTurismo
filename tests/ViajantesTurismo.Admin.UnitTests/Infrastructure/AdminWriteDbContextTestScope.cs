using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminWriteDbContextTestScope : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    private readonly AdminWriteDbContext dbContext;

    public AdminWriteDbContextTestScope(ServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.provider = provider;
        scope = provider.CreateScope();
        dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
    }

    public AdminWriteDbContext DbContext => dbContext;

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        scope.Dispose();
        await provider.DisposeAsync();
    }
}
