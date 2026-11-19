using Microsoft.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// Entity Framework persistence for generated document lineage aggregates.
/// </summary>
internal sealed class DocumentStore(AdminWriteDbContext dbContext) : IDocumentStore
{
    private const int PurgeBatchSize = 500;

    /// <summary>Stages a new lineage for insertion on the next save.</summary>
    public void Add(DocumentLineage lineage) => dbContext.DocumentLineages.Add(lineage);

    /// <summary>Loads the lineage with revision metadata and fields, excluding sealed artifact bodies.</summary>
    public async Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct) =>
        await dbContext.DocumentLineages
            .Include(lineage => lineage.Revisions.OrderBy(revision => revision.Revision))
            .ThenInclude(revision => revision.Fields.OrderBy(field => field.SortOrder))
            .AsSplitQuery()
            .SingleOrDefaultAsync(lineage => lineage.Revisions.Any(revision => revision.Id == documentId), ct);

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>?> ReadFinalizedArtifactContent(Guid documentId, CancellationToken ct)
    {
        var content = await QueryBody(dbContext, documentId)
            .Where(_ => dbContext.DocumentDrafts.Any(draft => draft.Id == documentId && draft.Status == DocumentStatus.Finalized))
            .SingleOrDefaultAsync(ct);
        return content is null ? null : new ReadOnlyMemory<byte>(content);
    }

    /// <summary>Projects the sealed bytes for one revision without tracking the artifact entity.</summary>
    internal static IQueryable<byte[]> QueryBody(AdminWriteDbContext context, Guid documentId) =>
        context.Set<DocumentArtifactBody>()
            .AsNoTracking()
            .Where(body => body.DocumentId == documentId)
            .Select(body => EF.Property<byte[]>(body, DocumentArtifactBodyConfiguration.ContentField));

    /// <summary>Deletes one batch of expired unfinalized drafts, bumps or removes affected lineages, and returns the removed draft count.</summary>
    public async Task<DocumentPurgeCounts> PurgeExpiredDrafts(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(heldLineages);

        return await EfCoreCommandTransactionScope.Execute(
            dbContext,
            () => PurgeExpiredDraftsCore(now, heldLineages, ct),
            ct);
    }

    private async ValueTask<DocumentPurgeCounts> PurgeExpiredDraftsCore(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct)
    {
        var activeLineages = await ActiveHoldLineages.Find(dbContext, ct);
        var expiredQuery = dbContext.DocumentDrafts
            .Where(document => document.FinalizedAt == null && document.RetentionExpiresAt <= now);
        var preHeldCount = activeLineages.Length == 0
            ? 0
            : await expiredQuery.CountAsync(document => activeLineages.Contains(document.DocumentLineageId), ct);
        var purgeCandidates = await expiredQuery
            .Where(document => !activeLineages.Contains(document.DocumentLineageId))
            .OrderBy(document => document.RetentionExpiresAt)
            .Select(document => new { document.Id, document.DocumentLineageId })
            .Take(PurgeBatchSize)
            .ToArrayAsync(ct);

        if (purgeCandidates.Length == 0)
        {
            return new DocumentPurgeCounts(0, preHeldCount);
        }

        var held = await heldLineages([.. purgeCandidates.Select(candidate => candidate.DocumentLineageId).Distinct()], ct);
        var heldCount = preHeldCount + purgeCandidates.Count(candidate => held.Contains(candidate.DocumentLineageId));
        purgeCandidates = [.. purgeCandidates.Where(candidate => !held.Contains(candidate.DocumentLineageId))];
        if (purgeCandidates.Length == 0)
        {
            return new DocumentPurgeCounts(0, heldCount);
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

        return new DocumentPurgeCounts(removedCount, heldCount);
    }
}
