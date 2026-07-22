using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeDocumentAuditStore : IDocumentAuditStore
{
    public List<DocumentAuditRecord> Records { get; } = [];

    public int PurgedCount { get; set; }

    public DateTime? PurgeCalledAt { get; private set; }

    public void Add(DocumentAuditRecord record) => Records.Add(record);

    public Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct)
    {
        PurgeCalledAt = now;
        return Task.FromResult(PurgedCount);
    }
}
