namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Describes differences between media metadata and object storage.
/// </summary>
/// <param name="MissingObjectKeys">Metadata keys with no stored object.</param>
/// <param name="OrphanObjectKeys">Stored object keys with no metadata reference.</param>
/// <param name="DeletedOrphanObjectKeys">Orphan object keys deleted by an explicit cleanup run.</param>
public sealed record MediaObjectReconciliationReport(
    IReadOnlyList<string> MissingObjectKeys,
    IReadOnlyList<string> OrphanObjectKeys,
    IReadOnlyList<string> DeletedOrphanObjectKeys);
