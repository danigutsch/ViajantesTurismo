using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Document audit store that keeps added records in memory for assertions.</summary>
internal sealed class CapturingDocumentAuditStore : IDocumentAuditStore
{
    private readonly List<DocumentAuditRecord> records = [];

    /// <summary>Gets the audit records added so far.</summary>
    public IReadOnlyList<DocumentAuditRecord> Records => records;

    /// <summary>Records the audit entry in memory.</summary>
    public void Add(DocumentAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        records.Add(record);
    }

    /// <summary>Ignores purge evidence.</summary>
    public void Add(DocumentAuditPurgeRecord record)
    {
    }

    /// <summary>Purges nothing and returns zero.</summary>
    public Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct) =>
        Task.FromResult(0);

    /// <summary>Has nothing staged to discard.</summary>
    public void DiscardPendingChanges()
    {
    }
}
