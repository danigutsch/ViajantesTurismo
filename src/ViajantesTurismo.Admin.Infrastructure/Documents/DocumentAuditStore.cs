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

    /// <summary>Deletes up to one batch of audit records whose retention expired at or before <paramref name="now"/> and returns the count.</summary>
    public async Task<int> PurgeExpiredRecords(DateTime now, CancellationToken ct)
    {
        var purgeIds = await dbContext.DocumentAuditRecords
            .Where(record => record.RetentionExpiresAt <= now)
            .OrderBy(record => record.RetentionExpiresAt)
            .Select(record => record.Id)
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        if (purgeIds.Length == 0)
        {
            return 0;
        }

        return await dbContext.DocumentAuditRecords
            .Where(record => purgeIds.Contains(record.Id) && record.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(ct);
    }
}
