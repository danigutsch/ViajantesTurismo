using Microsoft.EntityFrameworkCore;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Test scope that resolves the Admin write context and outbox from a provider and can lease a fresh context; disposing it disposes the scope and the provider.</summary>
internal sealed class AdminWriteDbContextTestScope : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private AsyncServiceScope scope;

    /// <summary>Creates the scope on the provider.</summary>
    public AdminWriteDbContextTestScope(ServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.provider = provider;
        scope = provider.CreateAsyncScope();
    }

    /// <summary>Gets the integration event outbox resolved from the current scope.</summary>
    public IIntegrationEventOutbox Outbox => scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

    /// <summary>Gets the Admin write context resolved from the current scope.</summary>
    public AdminWriteDbContext DbContext => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

    /// <summary>Returns the outbox messages saved in the current context, detaching each from the change tracker.</summary>
    public EventEnvelope[] ReadSavedOutboxMessages()
    {
        var context = DbContext;
        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is IIntegrationEventOutboxMessage)
            .ToArray();
        return [.. entries.Select(entry =>
        {
            entry.State.ShouldBe(EntityState.Unchanged);
            var message = (IIntegrationEventOutboxMessage)entry.Entity;
            entry.State = EntityState.Detached;
            return context.Find(entry.Metadata.ClrType, message.Id).ShouldBeAssignableTo<EventEnvelope>();
        })];
    }

    /// <summary>Disposes the current scope, starts a new one and returns its write context.</summary>
    public async ValueTask<AdminWriteDbContext> LeaseNextDbContext()
    {
        await scope.DisposeAsync().ConfigureAwait(false);
        scope = provider.CreateAsyncScope();

        return DbContext;
    }

    /// <summary>Disposes the current scope and the provider.</summary>
    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync().ConfigureAwait(false);
        await provider.DisposeAsync().ConfigureAwait(false);
    }
}
