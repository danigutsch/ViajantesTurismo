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

    /// <summary>Atomically removes unfinalized expired document revisions, except those of a held lineage.</summary>
    /// <param name="now">The expiration cutoff.</param>
    /// <param name="heldLineages">Reports, once for all candidate lineages, which lineages a disposition hold restrains.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The number of deleted revisions and the number skipped because their lineage is held.</returns>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    Task<DocumentPurgeCounts> PurgeExpiredDrafts(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct);
}
