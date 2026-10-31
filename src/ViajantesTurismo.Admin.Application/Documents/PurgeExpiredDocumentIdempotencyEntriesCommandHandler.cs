using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges document idempotency entries once their replay window has elapsed, except those of a held lineage.</summary>
public sealed class PurgeExpiredDocumentIdempotencyEntriesCommandHandler(
    IDocumentIdempotencyRetentionStore retentionStore,
    IDocumentDispositionTransaction transaction,
    DocumentHoldGate holdGate,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Removes expired, unheld document idempotency entries in one transaction with the hold check and returns the
    /// purged and held counts. An unknown hold state aborts the purge with an exception and removes nothing.
    /// </summary>
    public async Task<DocumentPurgeCounts> Handle(CancellationToken ct)
    {
        var cutoff = timeProvider.GetUtcNow() - DocumentCommandIdempotency.ReplayWindow;
        return await transaction.Execute([], () => retentionStore.PurgeExpiredEntries(cutoff, holdGate.FindHeldLineages, ct), ct);
    }
}
