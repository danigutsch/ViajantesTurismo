namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Persists immutable document audit records.</summary>
public interface IDocumentAuditStore
{
    /// <summary>Adds an immutable document audit record.</summary>
    void Add(DocumentAuditRecord record);

    /// <summary>Removes audit records whose approved retention period has elapsed.</summary>
    Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct);
}
