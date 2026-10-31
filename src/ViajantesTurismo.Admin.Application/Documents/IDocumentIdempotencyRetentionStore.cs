using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Removes document idempotency entries that can no longer replay.</summary>
public interface IDocumentIdempotencyRetentionStore
{
    /// <summary>
    /// Removes one batch of document idempotency entries whose completion, or start when never completed,
    /// is at or before <paramref name="cutoff" />, except entries of a document whose lineage
    /// <paramref name="heldLineages" /> reports as held. The filter is called once, with every candidate lineage.
    /// </summary>
    /// <returns>How many entries were removed and how many were skipped because their lineage is held.</returns>
    Task<DocumentPurgeCounts> PurgeExpiredEntries(DateTimeOffset cutoff, HeldDocumentLineages heldLineages, CancellationToken ct);
}
