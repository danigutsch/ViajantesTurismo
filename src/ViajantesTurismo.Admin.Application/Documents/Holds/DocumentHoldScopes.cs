using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Names the opaque disposition-hold scope of a document lineage: only a kind and the lineage identifier.</summary>
public static class DocumentHoldScopes
{
    /// <summary>The scope kind shared by every document lineage hold.</summary>
    public const string LineageKind = "document-lineage";

    private const string LineageKeyFormat = "N";

    /// <summary>Creates the scope of a lineage, keyed by its lowercase 32-digit identifier.</summary>
    /// <param name="lineageId">The document lineage identifier.</param>
    /// <returns>The hold scope.</returns>
    public static DispositionScope ForLineage(Guid lineageId) =>
        DispositionScope.From(LineageKind, lineageId.ToString(LineageKeyFormat));

    /// <summary>Reads the lineage identifier back from a lineage scope.</summary>
    /// <param name="scope">The scope of a stored hold.</param>
    /// <param name="lineageId">The lineage identifier when the scope is a lineage scope.</param>
    /// <returns><see langword="true" /> when the scope names a document lineage.</returns>
    public static bool TryGetLineageId(DispositionScope scope, out Guid lineageId)
    {
        lineageId = Guid.Empty;
        return string.Equals(scope.Kind, LineageKind, StringComparison.Ordinal)
            && Guid.TryParseExact(scope.Key, LineageKeyFormat, out lineageId);
    }
}
