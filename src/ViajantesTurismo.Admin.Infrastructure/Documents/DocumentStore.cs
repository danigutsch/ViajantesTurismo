using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Entity Framework persistence for generated document lineage aggregates.
/// </summary>
internal sealed class DocumentStore(AdminWriteDbContext dbContext) : IDocumentStore
{
    private const int PurgeBatchSize = 500;

    public void Add(DocumentLineage lineage) => dbContext.DocumentLineages.Add(lineage);

    public async Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct) =>
        await dbContext.DocumentLineages
            .Include(lineage => lineage.Revisions.OrderBy(revision => revision.Revision))
            .ThenInclude(revision => revision.Fields.OrderBy(field => field.SortOrder))
            .AsSplitQuery()
            .SingleOrDefaultAsync(lineage => lineage.Revisions.Any(revision => revision.Id == documentId), ct);

    public async Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct) =>
        await EfCoreCommandTransactionScope.Execute(
            dbContext,
            () => PurgeExpiredDraftsCore(now, ct),
            ct);

    private async ValueTask<int> PurgeExpiredDraftsCore(DateTime now, CancellationToken ct)
    {
        var purgeCandidates = await dbContext.DocumentDrafts
            .Where(document => document.FinalizedAt == null && document.RetentionExpiresAt <= now)
            .OrderBy(document => document.RetentionExpiresAt)
            .Select(document => new { document.Id, document.DocumentLineageId })
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        if (purgeCandidates.Length == 0)
        {
            return 0;
        }

        var purgeIds = purgeCandidates.Select(candidate => candidate.Id).ToArray();
        var affectedLineageIds = purgeCandidates
            .Select(candidate => candidate.DocumentLineageId)
            .Distinct()
            .ToArray();

        var removedCount = await dbContext.DocumentDrafts
            .Where(document => purgeIds.Contains(document.Id) && document.FinalizedAt == null && document.RetentionExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        if (removedCount > 0)
        {
            _ = await dbContext.DocumentLineages
                .Where(lineage => affectedLineageIds.Contains(lineage.Id) && lineage.Revisions.Any())
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(lineage => lineage.Version, lineage => lineage.Version + 1),
                    ct);

            _ = await dbContext.DocumentLineages
                .Where(lineage => affectedLineageIds.Contains(lineage.Id) && !lineage.Revisions.Any())
                .ExecuteDeleteAsync(ct);
        }

        return removedCount;
    }
}
