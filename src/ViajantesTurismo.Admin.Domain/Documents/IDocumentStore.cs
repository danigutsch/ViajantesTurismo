namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Persists generated document lineage aggregates.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Adds a document lineage aggregate.</summary>
    /// <param name="lineage">The lineage to add.</param>
    void Add(DocumentLineage lineage);

    /// <summary>Gets a lineage by one of its document revision identifiers.</summary>
    /// <param name="documentId">The document revision identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The owning lineage, or <see langword="null" /> when the revision does not exist.</returns>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct);

    /// <summary>Atomically removes unfinalized expired document revisions.</summary>
    /// <param name="now">The expiration cutoff.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The number of deleted revisions.</returns>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct);
}
