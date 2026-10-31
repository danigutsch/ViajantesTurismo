namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Persists immutable document audit records.</summary>
public interface IDocumentAuditStore
{
    /// <summary>Adds an immutable document audit record.</summary>
    void Add(DocumentAuditRecord record);

    /// <summary>Adds immutable evidence that an audit-retention purge ran.</summary>
    void Add(DocumentAuditPurgeRecord record);

    /// <summary>
    /// Stages removal of audit records whose currently configured retention period has elapsed, except those of a
    /// lineage <paramref name="heldLineages" /> reports as held; the removal commits on the next save. The filter is
    /// called once, with every candidate lineage, and its failure aborts the purge before anything is staged.
    /// </summary>
    Task<DocumentPurgeCounts> PurgeExpiredRecords(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct);

    /// <summary>Discards every change staged and not yet saved, so a failed commit cannot leak into later evidence.</summary>
    void DiscardPendingChanges();
}
