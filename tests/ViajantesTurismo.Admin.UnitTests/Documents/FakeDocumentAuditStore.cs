using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>In-memory document audit store fake.</summary>
internal sealed class FakeDocumentAuditStore : IDocumentAuditStore
{
    /// <summary>Audit records added to the store.</summary>
    public List<DocumentAuditRecord> Records { get; } = [];

    /// <summary>Count returned by <c>PurgeExpiredRecords</c>.</summary>
    public int PurgedCount
    {
        get; set;
    }

    /// <summary>Time passed to the last purge, or null when no purge ran.</summary>
    public DateTime? PurgeCalledAt
    {
        get; private set;
    }

    /// <summary>Adds the record to <see cref="Records" />.</summary>
    public void Add(DocumentAuditRecord record) => Records.Add(record);

    /// <summary>Captures the purge time and returns <see cref="PurgedCount" />.</summary>
    public Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct)
    {
        PurgeCalledAt = now;
        return Task.FromResult(PurgedCount);
    }
}
