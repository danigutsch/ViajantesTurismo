using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>Resolves configured document ids to lineages and lists holds from an in-memory store.</summary>
/// <param name="store">The hold store whose holds are listed.</param>
public sealed class FakeDocumentHoldQuery(InMemoryDispositionHoldStore store) : IDocumentHoldQuery
{
    private readonly List<IReadOnlyCollection<Guid>> lookups = [];

    /// <summary>Gets the lineage of each known document id.</summary>
    public Dictionary<Guid, Guid> LineageByDocument { get; } = [];

    /// <summary>Gets the lineage sets of every current-document lookup, one entry per call.</summary>
    public IReadOnlyList<IReadOnlyCollection<Guid>> CurrentDocumentLookups => lookups;

    /// <inheritdoc />
    public Task<Guid?> FindLineageId(Guid documentId, CancellationToken ct) =>
        Task.FromResult(LineageByDocument.TryGetValue(documentId, out var lineageId) ? (Guid?)lineageId : null);

    /// <inheritdoc />
    public Task<IReadOnlyList<Hold>> ListForLineage(Guid lineageId, CancellationToken ct)
    {
        var scope = DocumentHoldScopes.ForLineage(lineageId);
        IReadOnlyList<Hold> holds = [.. store.Holds.Where(hold => hold.Scope == scope)];
        return Task.FromResult(holds);
    }

    /// <inheritdoc />
    public Task<Hold?> FindForLineage(Guid lineageId, Guid holdId, CancellationToken ct)
    {
        var scope = DocumentHoldScopes.ForLineage(lineageId);
        return Task.FromResult(store.Holds.FirstOrDefault(hold => hold.Id == holdId && hold.Scope == scope));
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, Guid>> FindCurrentDocumentIds(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lineageIds);

        lookups.Add(lineageIds);
        IReadOnlyDictionary<Guid, Guid> current = LineageByDocument
            .Where(pair => lineageIds.Contains(pair.Value))
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Last().Key);
        return Task.FromResult(current);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Hold>> Search(DocumentHoldSearch search, int skip, int take, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(search);

        IReadOnlyList<Hold> holds = [.. search.Apply(store.Holds.AsQueryable()).Skip(skip).Take(take)];
        return Task.FromResult(holds);
    }
}
