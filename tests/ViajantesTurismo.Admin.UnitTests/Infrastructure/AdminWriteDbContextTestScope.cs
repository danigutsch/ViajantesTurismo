using Microsoft.EntityFrameworkCore;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
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

    public IIntegrationEventOutbox Outbox => scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

    public AdminWriteDbContext DbContext => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

    public EventEnvelope[] ReadSavedOutboxMessages()
    {
        var context = DbContext;
        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is IIntegrationEventOutboxMessage)
            .ToArray();
        return entries.Select(entry =>
        {
            entry.State.ShouldBe(EntityState.Unchanged);
            var message = (IIntegrationEventOutboxMessage)entry.Entity;
            entry.State = EntityState.Detached;
            return context.Find(entry.Metadata.ClrType, message.Id).ShouldBeAssignableTo<EventEnvelope>();
        }).ToArray();
    }

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
