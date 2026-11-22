using PandaBear.Mediator;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges document idempotency entries once their replay window has elapsed, except those of a held lineage.</summary>
public sealed class PurgeExpiredDocumentIdempotencyEntriesCommandHandler(
    IDocumentIdempotencyRetentionStore retentionStore,
    DocumentPurgeRunner runner,
    DocumentHoldGate holdGate,
    TimeProvider timeProvider) : ICommandHandler<PurgeExpiredDocumentIdempotencyEntriesCommand, DocumentPurgeCounts>
{
    /// <summary>
    /// Removes expired, unheld document idempotency entries in one transaction with the hold check and returns the
    /// purged and held counts, and saves counts-only evidence of the run in that transaction. An unknown hold state
    /// removes nothing, records refused evidence and rethrows.
    /// </summary>
    public async ValueTask<DocumentPurgeCounts> Handle(PurgeExpiredDocumentIdempotencyEntriesCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cutoff = timeProvider.GetUtcNow() - DocumentCommandIdempotency.ReplayWindow;
        return await runner.Run(
            DocumentRetentionPurgeKind.IdempotencyEntries,
            () => retentionStore.PurgeExpiredEntries(cutoff, holdGate.FindHeldLineages, ct),
            ct);
    }
}
