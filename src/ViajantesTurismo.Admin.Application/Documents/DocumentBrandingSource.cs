using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Captures the Branding snapshot for one document revision from the local Admin projection, failing closed while
/// the projection is missing or recovering.
/// </summary>
public sealed class DocumentBrandingSource(IBrandingProjectionStore projectionStore)
{
    internal const string UnavailableDetail =
        "Branding is not available for document generation yet. Retry after the Branding projection is ready.";

    internal async Task<Result<DocumentBrandingSnapshotValues>> Capture(CancellationToken ct)
    {
        // A recovering projection is fenced: documents fail closed instead of capturing a stale or partial snapshot.
        var recovery = await projectionStore.GetRecovery(ct);
        var current = recovery.IsReady ? await projectionStore.GetCurrent(ct) : null;
        return current is null
            ? Result.Unavailable<DocumentBrandingSnapshotValues>(UnavailableDetail)
            : Result.Ok(DocumentBrandingSnapshotFactory.FromProjection(current));
    }
}
