using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Captures the Branding snapshot for one document revision from the local Admin projection, failing closed while
/// the projection is missing or recovering.
/// </summary>
public sealed class DocumentBrandingSource(IBrandingProjectionStore projectionStore)
{
    /// <summary>Problem detail returned when no ready branding projection exists.</summary>
    internal const string UnavailableDetail =
        "Branding is not available for document generation yet. Retry after the Branding projection is ready.";

    /// <summary>Captures the current branding snapshot, or an unavailable failure when the projection is missing or recovering.</summary>
    /// <remarks>A recovering projection is fenced: documents fail closed instead of capturing a stale or partial snapshot.</remarks>
    internal async Task<Result<DocumentBrandingSnapshotValues>> Capture(CancellationToken ct)
    {
        var recovery = await projectionStore.GetRecovery(ct);
        var current = recovery.IsReady ? await projectionStore.GetCurrent(ct) : null;
        return current is null
            ? Result.Unavailable<DocumentBrandingSnapshotValues>(UnavailableDetail)
            : Result.Ok(DocumentBrandingSnapshotFactory.FromProjection(current));
    }
}
