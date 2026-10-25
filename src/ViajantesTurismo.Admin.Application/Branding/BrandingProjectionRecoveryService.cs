using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>
/// Operator entry points that fence the Admin Branding projection and authorize the single reseed that ends recovery.
/// Callers must run from an audited operator workflow; the reseed itself records the operator in the recovery audit.
/// </summary>
public sealed class BrandingProjectionRecoveryService(IBrandingProjectionStore store, TimeProvider timeProvider)
{
    /// <summary>Commits the recovery fence; ordinary delivery and projection-sourced documents stop until reseed.</summary>
    /// <param name="reason">Why recovery is needed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when the fence commits.</returns>
    public async Task Begin(string reason, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var recovery = await store.GetRecovery(ct).ConfigureAwait(false);
        if (!recovery.IsReady)
        {
            return;
        }

        store.SetRecovery(BrandingProjectionRecovery.Start(reason, timeProvider.GetUtcNow()));
        await store.Save(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Authorizes exactly one reseed event when the expected watermark still matches what Admin holds.
    /// </summary>
    /// <param name="request">The operator request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success, or the reason the authorization was refused.</returns>
    public async Task<Result> AuthorizeReseed(BrandingReseedRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IncidentReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Actor);

        var recovery = await store.GetRecovery(ct).ConfigureAwait(false);
        if (recovery.IsReady)
        {
            return Result.Conflict(detail: "Start recovery before authorizing a Branding reseed.");
        }

        if (request.EventId == Guid.Empty || request.TargetEpoch == Guid.Empty || request.TargetRevision <= 0)
        {
            return Result.Invalid(
                detail: "A reseed needs a non-empty event identity and epoch and a positive revision.",
                field: nameof(request.TargetRevision),
                message: "A reseed needs a non-empty event identity and epoch and a positive revision.");
        }

        var current = await store.GetCurrent(ct).ConfigureAwait(false);
        if (current?.SourceEpoch != request.ExpectedEpoch || current?.SourceRevision != request.ExpectedRevision)
        {
            return Result.Conflict(detail: "The Admin Branding watermark differs from the expected one; re-read it before authorizing.");
        }

        if (current is not null && request.TargetEpoch == current.SourceEpoch && request.TargetRevision < current.SourceRevision)
        {
            return Result.Invalid(
                detail: "A same-epoch reseed cannot move the revision backwards; rotate the source epoch instead.",
                field: nameof(request.TargetRevision),
                message: "A same-epoch reseed cannot move the revision backwards; rotate the source epoch instead.");
        }

        store.SetRecovery(recovery.Authorize(new BrandingReseedAuthorization(
            request.EventId,
            request.TargetEpoch,
            request.TargetRevision,
            request.Reason,
            request.IncidentReference,
            request.Actor,
            timeProvider.GetUtcNow())));
        await store.Save(ct).ConfigureAwait(false);
        return Result.Ok();
    }
}
