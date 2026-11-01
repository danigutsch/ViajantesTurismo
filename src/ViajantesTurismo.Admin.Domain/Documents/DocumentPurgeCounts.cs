namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Counts only: how many candidates a purge removed, how many a disposition hold made it skip and how many it kept for lack of a lineage owner.</summary>
/// <param name="Purged">The number of records removed.</param>
/// <param name="Held">The number of expired candidates skipped because a hold restrained their document lineage.</param>
/// <param name="Unattributed">The number of expired idempotency entries retained because they record no owning document lineage; always zero for other purges.</param>
public readonly record struct DocumentPurgeCounts(int Purged, int Held, int Unattributed = 0);
