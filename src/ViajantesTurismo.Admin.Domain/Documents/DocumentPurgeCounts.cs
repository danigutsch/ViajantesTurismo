namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Counts only: how many candidates a purge removed and how many a disposition hold made it skip.</summary>
/// <param name="Purged">The number of records removed.</param>
/// <param name="Undecidable">The number of expired candidates kept because no lineage could be determined, so no hold check can cover them.</param>
/// <param name="Held">The number of expired candidates skipped because a hold restrained their document lineage.</param>
public readonly record struct DocumentPurgeCounts(int Purged, int Held, int Undecidable = 0);
