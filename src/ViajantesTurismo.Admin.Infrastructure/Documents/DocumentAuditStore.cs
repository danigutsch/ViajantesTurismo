using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Entity Framework persistence for immutable document audit metadata.</summary>
internal sealed class DocumentAuditStore(AdminWriteDbContext dbContext) : IDocumentAuditStore
{
    private const int PurgeBatchSize = 500;

    /// <summary>Stages an audit record for insertion on the next save.</summary>
    public void Add(DocumentAuditRecord record) => dbContext.DocumentAuditRecords.Add(record);

    /// <summary>Stages purge evidence for insertion on the next save.</summary>
    public void Add(DocumentAuditPurgeRecord record) => dbContext.DocumentAuditPurgeRecords.Add(record);

    /// <summary>
    /// Stages deletion of up to one batch of audit records whose retention expired at or before <paramref name="now"/>
    /// and returns the count. The rows are removed on the next save, so the purge commits together with its evidence.
    /// The cutoff never exceeds the database clock: the retention trigger judges expiry by that clock and would otherwise
    /// fail the whole batch when the application clock is ahead.
    /// </summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredRecords(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(heldLineages);

        var databaseNow = await dbContext.Database
            .SqlQueryRaw<DateTime>("SELECT now() AS \"Value\"")
            .SingleAsync(ct);
        var cutoff = now < databaseNow ? now : databaseNow;
        var activeLineages = await ActiveHoldLineages.Find(dbContext, ct);
        Expression<Func<DocumentAuditRecord, bool>> isPreHeld = record =>
            activeLineages.Contains(EF.Property<Guid?>(record, DocumentAuditLineage.PropertyName));
        var expiredQuery = dbContext.DocumentAuditRecords.Where(record => record.RetentionExpiresAt <= cutoff);
        var preHeldCount = activeLineages.Length == 0 ? 0 : await expiredQuery.CountAsync(isPreHeld, ct);
        var candidateQuery = activeLineages.Length == 0
            ? expiredQuery
            : expiredQuery.Where(Expression.Lambda<Func<DocumentAuditRecord, bool>>(Expression.Not(isPreHeld.Body), isPreHeld.Parameters));
        var candidates = await candidateQuery
            .OrderBy(record => record.RetentionExpiresAt)
            .Take(PurgeBatchSize)
            .Select(record => new Candidate(record, EF.Property<Guid?>(record, DocumentAuditLineage.PropertyName)))
            .ToArrayAsync(ct);

        var held = await heldLineages([.. candidates.Where(candidate => candidate.Lineage is { } lineage && lineage != DocumentAuditLineage.NoLineage).Select(candidate => candidate.Lineage!.Value).Distinct()], ct);
        var purgeable = candidates
            .Where(candidate => candidate.Lineage is not { } lineage || lineage == DocumentAuditLineage.NoLineage || !held.Contains(lineage))
            .Select(candidate => candidate.Record)
            .ToArray();

        dbContext.DocumentAuditRecords.RemoveRange(purgeable);
        return new DocumentPurgeCounts(purgeable.Length, preHeldCount + candidates.Length - purgeable.Length);
    }

    private sealed record Candidate(DocumentAuditRecord Record, Guid? Lineage);

    /// <summary>Clears the change tracker, dropping staged deletions and unsaved evidence.</summary>
    public void DiscardPendingChanges() => dbContext.ChangeTracker.Clear();
}
