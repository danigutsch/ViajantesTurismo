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
}
