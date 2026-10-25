using Microsoft.Extensions.Options;
using PandaBear.Branding;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Captures the Branding snapshot for one document revision from the configured source. HTTP mode shadow-verifies
/// the local projection; projection mode reads only the projection and fails closed when it has none.
/// </summary>
public sealed class DocumentBrandingSource(
    IBrandingApiClient brandingApiClient,
    IBrandingProjectionStore projectionStore,
    IOptions<DocumentBrandingOptions> options)
{
    internal const string UnavailableDetail =
        "Branding is not available for document generation yet. Retry after the Branding projection is ready.";

    internal async Task<Result<DocumentBrandingSnapshotValues>> Capture(CancellationToken ct)
    {
        if (options.Value.Source == DocumentBrandingSourceMode.Projection)
        {
            // A recovering projection is fenced: documents fail closed instead of capturing a stale or partial snapshot.
            var recovery = await projectionStore.GetRecovery(ct);
            var current = recovery.IsReady ? await projectionStore.GetCurrent(ct) : null;
            return current is null
                ? Result.Unavailable<DocumentBrandingSnapshotValues>(UnavailableDetail)
                : Result.Ok(DocumentBrandingSnapshotFactory.FromProjection(current));
        }

        var projection = await projectionStore.GetCurrent(ct);
        var captured = DocumentBrandingSnapshotFactory.FromSettings(await brandingApiClient.GetPublicSettings(ct));
        if (projection is null)
        {
            DocumentBrandingMetrics.RecordProjectionMismatch(DocumentBrandingMetrics.ReasonMissing);
            return Result.Ok(captured);
        }

        var projected = DocumentBrandingSnapshotFactory.FromProjection(projection);
        if (!string.Equals(projected.Version, captured.Version, StringComparison.Ordinal))
        {
            DocumentBrandingMetrics.RecordProjectionMismatch(DocumentBrandingMetrics.ReasonDifferent);
            return Result.Ok(captured);
        }

        return Result.Ok(captured with
        {
            SourceRevision = projection.SourceRevision
        });
    }
}
