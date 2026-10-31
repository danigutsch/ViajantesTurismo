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
        var heldDocuments = dbContext.DocumentDrafts
            .Where(draft => activeLineages.Contains(draft.DocumentLineageId))
            .Select(draft => draft.Id);
        Expression<Func<DocumentAuditRecord, bool>> isPreHeld = record =>
            activeLineages.Contains(EF.Property<Guid?>(record, DocumentAuditLineage.PropertyName))
            || (record.DocumentId != null && heldDocuments.Contains(record.DocumentId.Value));
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

        var lineageByDocument = await FindLineages(candidates, ct);
        var resolved = candidates
            .Select(candidate => (candidate.Record, Lineage: candidate.Lineage ?? ResolveLineage(candidate.Record, lineageByDocument)))
            .ToArray();
        var held = await heldLineages([.. resolved.Where(item => item.Lineage is { } lineage && lineage != DocumentAuditLineage.NoLineage).Select(item => item.Lineage!.Value).Distinct()], ct);
        var purgeable = resolved
            .Where(item => IsDecidablyUnheld(item.Record, item.Lineage, held))
            .Select(item => item.Record)
            .ToArray();

        dbContext.DocumentAuditRecords.RemoveRange(purgeable);
        return new DocumentPurgeCounts(purgeable.Length, preHeldCount + candidates.Length - purgeable.Length);
    }

    private async Task<Dictionary<Guid, Guid>> FindLineages(Candidate[] candidates, CancellationToken ct)
    {
        var documentIds = candidates
            .Where(candidate => candidate.Lineage is null && candidate.Record.DocumentId is not null)
            .Select(candidate => candidate.Record.DocumentId!.Value)
            .Distinct()
            .ToArray();
        return await dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => documentIds.Contains(draft.Id))
            .Select(draft => new { draft.Id, draft.DocumentLineageId })
            .ToDictionaryAsync(draft => draft.Id, draft => draft.DocumentLineageId, ct);
    }

    private static Guid? ResolveLineage(DocumentAuditRecord record, Dictionary<Guid, Guid> lineageByDocument) =>
        record.DocumentId is { } documentId && lineageByDocument.TryGetValue(documentId, out var lineageId) ? lineageId : null;

    private static bool IsDecidablyUnheld(DocumentAuditRecord record, Guid? lineage, IReadOnlySet<Guid> held) =>
        lineage is { } lineageId ? lineageId == DocumentAuditLineage.NoLineage || !held.Contains(lineageId) : record.DocumentId is null;

    private sealed record Candidate(DocumentAuditRecord Record, Guid? Lineage);

    /// <summary>Clears the change tracker, dropping staged deletions and unsaved evidence.</summary>
    public void DiscardPendingChanges() => dbContext.ChangeTracker.Clear();
}
