using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges only document revisions eligible under draft retention policy and not restrained by a hold.</summary>
public sealed class PurgeExpiredDraftsCommandHandler(
    IDocumentStore documentStore,
    DocumentPurgeRunner runner,
    DocumentHoldGate holdGate,
    TimeProvider timeProvider) : ICommandHandler<PurgeExpiredDraftsCommand, DocumentPurgeCounts>
{
    /// <summary>
    /// Removes expired unfinalized drafts of unheld lineages in one transaction with the hold check and returns the
    /// purged and held counts, and saves counts-only evidence of the run in that transaction. An unknown hold state
    /// removes nothing, records refused evidence and rethrows.
    /// </summary>
    public async ValueTask<DocumentPurgeCounts> Handle(PurgeExpiredDraftsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await runner.Run(
            DocumentRetentionPurgeKind.Drafts,
            () => documentStore.PurgeExpiredDrafts(now, holdGate.FindHeldLineages, ct),
            ct);
    }
}
