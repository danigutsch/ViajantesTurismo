using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Entity Framework persistence for generated document revisions.
/// </summary>
internal sealed class DocumentStore(AdminWriteDbContext dbContext) : IDocumentStore
{
    private const int PurgeBatchSize = 500;

    public void Add(DocumentDraft document) => dbContext.DocumentDrafts.Add(document);

    public async Task<DocumentDraft?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.DocumentDrafts
            .Include(document => document.Fields.OrderBy(field => field.SortOrder))
            .FirstOrDefaultAsync(document => document.Id == id, ct);

    public async Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        var purgeIds = await dbContext.DocumentDrafts
            .Where(document => document.FinalizedAt == null && document.RetentionExpiresAt <= now)
            .OrderBy(document => document.RetentionExpiresAt)
            .Select(document => document.Id)
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        if (purgeIds.Length == 0)
        {
            return 0;
        }

        return await dbContext.DocumentDrafts
            .Where(document => purgeIds.Contains(document.Id) && document.FinalizedAt == null && document.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(ct);
    }
}
