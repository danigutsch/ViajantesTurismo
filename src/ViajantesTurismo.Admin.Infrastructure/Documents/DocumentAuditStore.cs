using Microsoft.EntityFrameworkCore;
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
        var expired = await dbContext.DocumentAuditRecords
            .Where(record => record.RetentionExpiresAt <= cutoff)
            .OrderBy(record => record.RetentionExpiresAt)
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        var lineageByDocument = await FindLineages(expired, ct);
        var held = await heldLineages([.. lineageByDocument.Values.Distinct()], ct);
        var purgeable = expired
            .Where(record => !IsHeld(record, lineageByDocument, held))
            .ToArray();

        dbContext.DocumentAuditRecords.RemoveRange(purgeable);
        return new DocumentPurgeCounts(purgeable.Length, expired.Length - purgeable.Length);
    }

    private async Task<Dictionary<Guid, Guid>> FindLineages(DocumentAuditRecord[] records, CancellationToken ct)
    {
        var documentIds = records.Where(record => record.DocumentId is not null).Select(record => record.DocumentId!.Value).Distinct().ToArray();
        return await dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => documentIds.Contains(draft.Id))
            .Select(draft => new { draft.Id, draft.DocumentLineageId })
            .ToDictionaryAsync(draft => draft.Id, draft => draft.DocumentLineageId, ct);
    }

    private static bool IsHeld(DocumentAuditRecord record, Dictionary<Guid, Guid> lineageByDocument, IReadOnlySet<Guid> held) =>
        record.DocumentId is { } documentId
        && lineageByDocument.TryGetValue(documentId, out var lineageId)
        && held.Contains(lineageId);

    /// <summary>Clears the change tracker, dropping staged deletions and unsaved evidence.</summary>
    public void DiscardPendingChanges() => dbContext.ChangeTracker.Clear();
}
