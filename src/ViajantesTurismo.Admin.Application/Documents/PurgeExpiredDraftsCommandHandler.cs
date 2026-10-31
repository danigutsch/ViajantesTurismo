using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges only document revisions eligible under draft retention policy and not restrained by a hold.</summary>
public sealed class PurgeExpiredDraftsCommandHandler(
    IDocumentStore documentStore,
    IDocumentDispositionTransaction transaction,
    DocumentHoldGate holdGate,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Removes expired unfinalized drafts of unheld lineages in one transaction with the hold check and returns the
    /// purged and held counts. An unknown hold state aborts the purge with an exception and removes nothing.
    /// </summary>
    public async Task<DocumentPurgeCounts> Handle(PurgeExpiredDraftsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await transaction.Execute([], () => documentStore.PurgeExpiredDrafts(now, holdGate.FindHeldLineages, ct), ct);
    }
}
