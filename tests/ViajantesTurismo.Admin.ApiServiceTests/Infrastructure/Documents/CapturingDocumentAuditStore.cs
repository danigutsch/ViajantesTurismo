using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

internal sealed class CapturingDocumentAuditStore : IDocumentAuditStore
{
    private readonly List<DocumentAuditRecord> records = [];

    public IReadOnlyList<DocumentAuditRecord> Records => records;

    public void Add(DocumentAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        records.Add(record);
    }

    public Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct) =>
        Task.FromResult(0);
}
