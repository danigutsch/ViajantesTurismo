using Microsoft.EntityFrameworkCore;
using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

/// <summary>Reads document lineages and their holds from the Admin write context without tracking.</summary>
internal sealed class DocumentHoldQuery(AdminWriteDbContext dbContext) : IDocumentHoldQuery
{
    /// <inheritdoc />
    public async Task<Guid?> FindLineageId(Guid documentId, CancellationToken ct) =>
        await dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => draft.Id == documentId)
            .Select(draft => (Guid?)draft.DocumentLineageId)
            .SingleOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Hold>> ListForLineage(Guid lineageId, CancellationToken ct)
    {
        var scope = DocumentHoldScopes.ForLineage(lineageId);
        var holds = await dbContext.Set<Hold>()
            .AsNoTracking()
            .Where(hold => hold.Scope == scope)
            .ToListAsync(ct);
        return [.. holds.OrderBy(hold => hold.PlacedAt).ThenBy(hold => hold.Id)];
    }

    /// <inheritdoc />
    public async Task<Hold?> FindForLineage(Guid lineageId, Guid holdId, CancellationToken ct)
    {
        var scope = DocumentHoldScopes.ForLineage(lineageId);
        return await dbContext.Set<Hold>()
            .AsNoTracking()
            .Where(hold => hold.Id == holdId && hold.Scope == scope)
            .SingleOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid>> FindCurrentDocumentIds(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lineageIds);

        if (lineageIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var newest = await dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => lineageIds.Contains(draft.DocumentLineageId))
            .GroupBy(draft => draft.DocumentLineageId)
            .Select(group => group
                .OrderByDescending(draft => draft.Revision)
                .Select(draft => new { draft.DocumentLineageId, draft.Id })
                .First())
            .ToListAsync(ct);
        return newest.ToDictionary(row => row.DocumentLineageId, row => row.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Hold>> Search(DocumentHoldSearch search, int skip, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(search);

        return await search.Apply(dbContext.Set<Hold>().AsNoTracking())
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }
}
