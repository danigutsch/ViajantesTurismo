using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Entity Framework persistence for generated document revisions.
/// </summary>
internal sealed class DocumentStore(AdminWriteDbContext dbContext) : IDocumentStore
{
    public void Add(DocumentDraft document) => dbContext.DocumentDrafts.Add(document);

    public async Task<DocumentDraft?> GetById(Guid id, CancellationToken ct) =>
        await dbContext.DocumentDrafts
            .Include(document => document.Fields)
            .FirstOrDefaultAsync(document => document.Id == id, ct);

    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct) =>
        dbContext.DocumentDrafts
            .Where(document => document.FinalizedAt == null && document.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(ct);
}
