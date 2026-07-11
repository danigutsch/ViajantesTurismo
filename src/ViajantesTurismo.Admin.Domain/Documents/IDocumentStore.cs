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

    /// <summary>Gets unfinalized expired document revisions.</summary>
    Task<IReadOnlyList<DocumentDraft>> GetExpiredDrafts(DateTime now, CancellationToken ct);

    /// <summary>Removes an expired document revision.</summary>
    void Remove(DocumentDraft document);
}
