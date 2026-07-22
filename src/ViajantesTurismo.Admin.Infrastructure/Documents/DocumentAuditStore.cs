using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Entity Framework persistence for immutable document audit metadata.</summary>
internal sealed class DocumentAuditStore(AdminWriteDbContext dbContext) : IDocumentAuditStore
{
    private const int PurgeBatchSize = 500;

    public void Add(DocumentAuditRecord record) => dbContext.DocumentAuditRecords.Add(record);

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
