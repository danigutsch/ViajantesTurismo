using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class EfIdempotencyStore(CatalogDbContext dbContext) : IIdempotencyStore
{
    public async ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct)
    {
        var existing = await Find(operation, ct).ConfigureAwait(false);
        if (existing is null)
        {
            var entry = new IdempotencyEntryEntity
            {
                Scope = operation.Scope.Value,
                Key = operation.Key.Value,
                State = IdempotencyEntryState.Started,
                StartedAt = startedAt,
            };

            dbContext.IdempotencyInbox.Add(entry);
            try
            {
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                var concurrentEntry = await FindStartedByConcurrentCaller(operation, entry, ct).ConfigureAwait(false);
                if (concurrentEntry is null)
                {
                    throw;
                }

                return IdempotencyStartResult.AlreadyStarted(ToEntry(concurrentEntry));
            }

            return IdempotencyStartResult.StartedNew();
        }

        if (existing.State is IdempotencyEntryState.Completed || !IsExpired(existing, startedAt, lockDuration))
        {
            return IdempotencyStartResult.AlreadyStarted(ToEntry(existing));
        }

        existing.State = IdempotencyEntryState.Started;
        existing.StartedAt = startedAt;
        existing.CompletedAt = null;
        existing.ResultFingerprint = null;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return IdempotencyStartResult.StartedNew();
    }

    public async ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct)
    {
        var existing = await Find(operation, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Idempotency operation must be started before it can be completed.");

        existing.State = IdempotencyEntryState.Completed;
        existing.CompletedAt = completedAt;
        existing.ResultFingerprint = resultFingerprint;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask<IdempotencyEntry?> Get(IdempotencyOperation operation, CancellationToken ct)
    {
        var existing = await Find(operation, ct).ConfigureAwait(false);

        return existing is null ? null : ToEntry(existing);
    }

    private ValueTask<IdempotencyEntryEntity?> Find(IdempotencyOperation operation, CancellationToken ct) =>
        dbContext.IdempotencyInbox.FindAsync([operation.Scope.Value, operation.Key.Value], ct);

    private async ValueTask<IdempotencyEntryEntity?> FindStartedByConcurrentCaller(
        IdempotencyOperation operation,
        IdempotencyEntryEntity entry,
        CancellationToken ct)
    {
        dbContext.Entry(entry).State = EntityState.Detached;
        return await Find(operation, ct).ConfigureAwait(false);
    }

    private static bool IsExpired(
        IdempotencyEntryEntity existing,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration) =>
        existing.State is IdempotencyEntryState.Started
        && lockDuration is not null
        && existing.StartedAt.Add(lockDuration.Value) <= startedAt;

    private static IdempotencyEntry ToEntry(IdempotencyEntryEntity entity) =>
        new(
            new IdempotencyOperation(
                IdempotencyScope.From(entity.Scope),
                IdempotencyKey.From(entity.Key)),
            entity.State,
            entity.StartedAt,
            entity.CompletedAt,
            entity.ResultFingerprint);
}
