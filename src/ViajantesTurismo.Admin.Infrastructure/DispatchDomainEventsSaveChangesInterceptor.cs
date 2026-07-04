using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;
using SharedKernel.DomainEvents;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class DispatchDomainEventsSaveChangesInterceptor(
    IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private readonly List<IAggregateRoot> _aggregatesToClear = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context, CancellationToken.None).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearDomainEvents();

        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearDomainEvents();

        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ForgetTrackedAggregates();
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ForgetTrackedAggregates();

        return Task.CompletedTask;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async ValueTask DispatchDomainEvents(DbContext? dbContext, CancellationToken ct)
    {
        if (dbContext is null)
        {
            return;
        }

        var aggregateEntries = GetAggregateEntries(dbContext);
        var aggregates = aggregateEntries
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.GetDomainEvents().Count > 0)
            .ToArray();

        _aggregatesToClear.Clear();
        _aggregatesToClear.AddRange(aggregates);

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.GetDomainEvents())
            .ToArray();

        var domainEventDispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        foreach (var domainEvent in domainEvents)
        {
            await domainEventDispatcher.Dispatch(domainEvent, ct).ConfigureAwait(false);
        }
    }

    private void ClearDomainEvents()
    {
        foreach (var aggregate in _aggregatesToClear)
        {
            aggregate.ClearDomainEvents();
        }

        _aggregatesToClear.Clear();
    }

    private void ForgetTrackedAggregates()
    {
        _aggregatesToClear.Clear();
    }

    private static EntityEntry<IAggregateRoot>[] GetAggregateEntries(DbContext dbContext) =>
        dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
}
