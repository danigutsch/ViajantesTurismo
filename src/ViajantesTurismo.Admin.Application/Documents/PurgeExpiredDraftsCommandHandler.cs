using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges only document revisions eligible under draft retention policy.</summary>
public sealed class PurgeExpiredDraftsCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Removes expired unfinalized drafts and returns the count.</summary>
    public async Task<Result<int>> Handle(PurgeExpiredDraftsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expired = await documentStore.GetExpiredDrafts(now, ct);
        foreach (var document in expired.Where(document => document.IsExpiredDraft(now)))
        {
            documentStore.Remove(document);
        }

        if (expired.Count > 0)
        {
            await unitOfWork.SaveEntities(ct);
        }

        return Result.Ok(expired.Count);
    }
}
