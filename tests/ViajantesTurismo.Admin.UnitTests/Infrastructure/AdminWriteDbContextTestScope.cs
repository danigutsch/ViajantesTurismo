using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminWriteDbContextTestScope : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private AsyncServiceScope scope;

    public AdminWriteDbContextTestScope(ServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.provider = provider;
        scope = provider.CreateAsyncScope();
    }

    public AdminWriteDbContext DbContext => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

    public async ValueTask<AdminWriteDbContext> LeaseNextDbContext()
    {
        await scope.DisposeAsync().ConfigureAwait(false);
        scope = provider.CreateAsyncScope();

        return DbContext;
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync().ConfigureAwait(false);
        await provider.DisposeAsync().ConfigureAwait(false);
    }
}
