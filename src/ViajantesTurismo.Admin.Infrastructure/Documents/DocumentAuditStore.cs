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
    /// </summary>
    public async Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct)
    {
        var expired = await dbContext.DocumentAuditRecords
            .Where(record => record.RetentionExpiresAt <= now)
            .OrderBy(record => record.RetentionExpiresAt)
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        dbContext.DocumentAuditRecords.RemoveRange(expired);
        return expired.Length;
    }
}
