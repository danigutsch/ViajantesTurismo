namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Persists immutable document audit records.</summary>
public interface IDocumentAuditStore
{
    /// <summary>Adds an immutable document audit record.</summary>
    void Add(DocumentAuditRecord record);

    /// <summary>Adds immutable evidence that an audit-retention purge ran.</summary>
    void Add(DocumentAuditPurgeRecord record);

    /// <summary>Stages removal of audit records whose currently configured retention period has elapsed; the removal commits on the next save.</summary>
    Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct);

    /// <summary>Discards every change staged and not yet saved, so a failed commit cannot leak into later evidence.</summary>
    void DiscardPendingChanges();
}
