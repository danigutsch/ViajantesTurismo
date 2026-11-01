using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Reads the holds of a document lineage.</summary>
public interface IDocumentHoldQuery
{
    /// <summary>Finds the lineage that contains a document revision.</summary>
    /// <param name="documentId">Any revision of the lineage.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The lineage identifier, or <see langword="null" /> when the document does not exist.</returns>
    Task<Guid?> FindLineageId(Guid documentId, CancellationToken ct);

    /// <summary>Lists every hold, released or not, whose scope is the lineage, oldest first.</summary>
    /// <param name="lineageId">The document lineage identifier.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The holds.</returns>
    Task<IReadOnlyList<Hold>> ListForLineage(Guid lineageId, CancellationToken ct);

    /// <summary>Finds one hold, released or not, whose scope is the lineage.</summary>
    /// <param name="lineageId">The document lineage identifier.</param>
    /// <param name="holdId">The hold identifier.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The hold, or <see langword="null" /> when no such hold belongs to the lineage.</returns>
    Task<Hold?> FindForLineage(Guid lineageId, Guid holdId, CancellationToken ct);

    /// <summary>Searches the holds of every document lineage in the stable order of the search.</summary>
    /// <param name="search">The filters and order.</param>
    /// <param name="skip">The number of matching holds to skip.</param>
    /// <param name="take">The maximum number of holds to return.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The matching holds.</returns>
    Task<IReadOnlyList<Hold>> Search(DocumentHoldSearch search, int skip, int take, CancellationToken ct);

    /// <summary>Resolves the newest revision of each lineage in one read.</summary>
    /// <param name="lineageIds">The document lineage identifiers.</param>
    /// <param name="ct">A token that can cancel the operation.</param>
    /// <returns>The newest document id per lineage; a lineage with no remaining document is absent.</returns>
    Task<IReadOnlyDictionary<Guid, Guid>> FindCurrentDocumentIds(IReadOnlyCollection<Guid> lineageIds, CancellationToken ct);
}
