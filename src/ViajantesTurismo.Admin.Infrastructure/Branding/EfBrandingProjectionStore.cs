using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>
/// EF Core implementation of <see cref="IBrandingProjectionStore" /> over the write context.
/// </summary>
/// <param name="dbContext">Write context that tracks and commits the projection rows.</param>
internal sealed class EfBrandingProjectionStore(AdminWriteDbContext dbContext) : IBrandingProjectionStore
{
    /// <summary>Loads the current projection state; null when none has been stored.</summary>
    public async Task<BrandingProjectionState?> GetCurrent(CancellationToken ct)
    {
        var record = await Load(ct).ConfigureAwait(false);
        return record?.ToState();
    }

    /// <summary>Returns whether an event id is already in the inbox.</summary>
    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) =>
        dbContext.Set<BrandingInboxRecord>().AnyAsync(message => message.EventId == eventId, ct);

    /// <summary>Stages replacement of the projection state, adding the row when none is tracked.</summary>
    public void Replace(BrandingProjectionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var tracked = dbContext.Set<BrandingProjectionRecord>().Local
            .SingleOrDefault(record => record.Id == BrandingProjectionRecord.ProjectionId);
        if (tracked is null)
        {
            dbContext.Set<BrandingProjectionRecord>().Add(BrandingProjectionRecord.Create(state));
            return;
        }

        tracked.ReplaceWith(state);
    }

    /// <summary>Loads the recovery fence, or the ready state when no row exists.</summary>
    public async Task<BrandingProjectionRecovery> GetRecovery(CancellationToken ct)
    {
        var record = await dbContext.Set<BrandingRecoveryRecord>()
            .SingleOrDefaultAsync(recovery => recovery.Id == BrandingRecoveryRecord.RecoveryId, ct)
            .ConfigureAwait(false);
        return record?.ToRecovery() ?? BrandingProjectionRecovery.Ready;
    }

    /// <summary>Returns whether the epoch was left behind by a recorded epoch transition.</summary>
    public Task<bool> IsRetiredEpoch(Guid sourceEpoch, CancellationToken ct) =>
        dbContext.Set<BrandingRecoveryAuditRecord>().AnyAsync(
            audit => audit.Kind == BrandingRecoveryAuditKind.EpochTransition && audit.OldEpoch == sourceEpoch,
            ct);

    /// <summary>Stages the recovery fence, adding the row when none is tracked.</summary>
    public void SetRecovery(BrandingProjectionRecovery recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        var tracked = dbContext.Set<BrandingRecoveryRecord>().Local
            .SingleOrDefault(record => record.Id == BrandingRecoveryRecord.RecoveryId);
        if (tracked is null)
        {
            // GetRecovery tracks an existing row, so an untracked fence is the first one ever written.
            dbContext.Set<BrandingRecoveryRecord>().Add(BrandingRecoveryRecord.Create(recovery));
            return;
        }

        tracked.ReplaceWith(recovery);
    }

    /// <summary>Stages a recovery audit entry.</summary>
    public void AppendAudit(BrandingRecoveryAuditEntry entry) =>
        dbContext.Set<BrandingRecoveryAuditRecord>().Add(BrandingRecoveryAuditRecord.From(entry));

    /// <summary>Stages an inbox entry for a processed event.</summary>
    public void Record(BrandingInboxEntry entry) => dbContext.Set<BrandingInboxRecord>().Add(BrandingInboxRecord.From(entry));

    // Projection replacement and inbox completion commit in one transaction. A concurrent worker that applied first
    // fails this save on the projection revision token or the inbox key; the transport then redelivers and the
    // inbox check makes the retry a no-op.
    /// <summary>Commits all staged projection, recovery, audit and inbox changes.</summary>
    public Task Save(CancellationToken ct) => dbContext.SaveChangesAsync(ct);

    private Task<BrandingProjectionRecord?> Load(CancellationToken ct) =>
        dbContext.Set<BrandingProjectionRecord>()
            .SingleOrDefaultAsync(record => record.Id == BrandingProjectionRecord.ProjectionId, ct);
}
