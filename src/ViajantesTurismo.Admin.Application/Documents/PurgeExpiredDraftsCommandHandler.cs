using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges only document revisions eligible under draft retention policy.</summary>
public sealed class PurgeExpiredDraftsCommandHandler(
    IDocumentStore documentStore,
    TimeProvider timeProvider)
{
    /// <summary>Removes expired unfinalized drafts and returns the count.</summary>
    public async Task<int> Handle(PurgeExpiredDraftsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var removedCount = await documentStore.PurgeExpiredDrafts(now, ct);

        return removedCount;
    }
}
