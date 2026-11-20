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
        var content = await ReadVerifiedArtifactContent(
            dbContext, documentId, dbContext.DocumentDrafts.Where(draft => draft.Status == DocumentStatus.Finalized), ct);
        if (content is null)
        {
            return null;
        }

        return new ReadOnlyMemory<byte>(content);
    }

    /// <summary>Reads sealed bytes only when they match the integrity metadata of an eligible revision.</summary>
    internal static async Task<byte[]?> ReadVerifiedArtifactContent(
        AdminWriteDbContext context,
        Guid documentId,
        IQueryable<DocumentDraft> eligibleRevisions,
        CancellationToken ct)
    {
        var artifact = await (
            from body in context.Set<DocumentArtifactBody>()
            join revision in eligibleRevisions on body.DocumentId equals revision.Id
            let content = EF.Property<byte[]>(body, DocumentArtifactBodyConfiguration.ContentField)
            where body.DocumentId == documentId
                && revision.FinalizedArtifactLength > 0
                && revision.FinalizedArtifactLength == content.Length
                && revision.FinalizedArtifactSha256 != null
            select new
            {
                Content = content,
                revision.FinalizedArtifactSha256
            })
            .SingleOrDefaultAsync(ct);

        if (artifact is null)
        {
            return null;
        }

        var digest = DocumentArtifactBody.ComputeSha256(artifact.Content);
        return string.Equals(digest, artifact.FinalizedArtifactSha256, StringComparison.Ordinal) ? artifact.Content : null;
    }

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
