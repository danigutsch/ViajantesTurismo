namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Persists generated document revisions.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Adds a document revision.</summary>
    void Add(DocumentDraft document);

    /// <summary>Gets a document revision by identifier.</summary>
    Task<DocumentDraft?> GetById(Guid id, CancellationToken ct);

    /// <summary>Atomically removes unfinalized expired document revisions.</summary>
    Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct);
}
