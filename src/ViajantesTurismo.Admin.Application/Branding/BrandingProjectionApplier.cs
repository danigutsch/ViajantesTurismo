using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>
/// Applies Branding revisions to the Admin projection. A deterministic rejection is recorded and acknowledged so it
/// never blocks later revisions; infrastructure failures propagate so the transport retries.
/// </summary>
public sealed class BrandingProjectionApplier(
    IBrandingProjectionStore store,
    TimeProvider timeProvider)
{
    /// <summary>Applies one delivered Branding revision event.</summary>
    /// <remarks>A pending event from a lineage an authorized epoch transition retired is quarantined, never recovered.</remarks>
    /// <param name="integrationEvent">The Branding revision event.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when the outcome commits.</returns>
    public async Task Apply(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await store.WasProcessed(integrationEvent.EventId, ct).ConfigureAwait(false))
        {
            return;
        }

        if (await store.IsRetiredEpoch(integrationEvent.SourceEpoch, ct).ConfigureAwait(false))
        {
            await Settle(integrationEvent, BrandingInboxOutcome.Rejected, "superseded-epoch", ct).ConfigureAwait(false);
            return;
        }

        var recovery = await store.GetRecovery(ct).ConfigureAwait(false);
        var current = await store.GetCurrent(ct).ConfigureAwait(false);
        if (!recovery.IsReady)
        {
            if (recovery.Authorization is { } authorization
                && authorization.Matches(integrationEvent.EventId, integrationEvent.SourceEpoch, integrationEvent.SourceRevision))
            {
                await Reseed(current, integrationEvent, authorization, ct).ConfigureAwait(false);
                return;
            }

            throw new BrandingProjectionRecoveringException();
        }

        var decision = BrandingProjectionRules.Decide(current, integrationEvent);
        if (decision is BrandingProjectionDecision.EpochMismatch or BrandingProjectionDecision.RevisionConflict)
        {
            var reason = decision == BrandingProjectionDecision.EpochMismatch ? "epoch-mismatch" : "revision-conflict";
            store.SetRecovery(BrandingProjectionRecovery.Start(reason, timeProvider.GetUtcNow()));
            await store.Save(ct).ConfigureAwait(false);
            throw new BrandingProjectionRecoveringException(
                $"The Branding projection entered recovery ({reason}); the event stays pending until an authorized reseed.");
        }

        var (outcome, reasonCode) = decision switch
        {
            BrandingProjectionDecision.Apply => (BrandingInboxOutcome.Applied, null),
            BrandingProjectionDecision.AlreadyApplied or BrandingProjectionDecision.Stale => (BrandingInboxOutcome.Stale, null),
            BrandingProjectionDecision.InvalidEnvelope => (BrandingInboxOutcome.Rejected, "invalid-envelope"),
            _ => throw new InvalidOperationException($"Unsupported Branding projection decision '{decision}'.")
        };

        if (decision == BrandingProjectionDecision.Apply)
        {
            store.Replace(BrandingProjectionState.From(integrationEvent));
        }

        await Settle(integrationEvent, outcome, reasonCode, ct).ConfigureAwait(false);
    }

    private async Task Reseed(
        BrandingProjectionState? current,
        BrandingSettingsChangedIntegrationEvent reseed,
        BrandingReseedAuthorization authorization,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var kind = current is not null && current.SourceEpoch == reseed.SourceEpoch
            ? BrandingRecoveryAuditKind.SameEpochReseed
            : BrandingRecoveryAuditKind.EpochTransition;
        store.Replace(BrandingProjectionState.From(reseed));
        store.AppendAudit(new BrandingRecoveryAuditEntry(
            Guid.CreateVersion7(),
            kind,
            current?.SourceEpoch,
            current?.SourceRevision,
            reseed.SourceEpoch,
            reseed.SourceRevision,
            reseed.EventId,
            authorization.Reason,
            authorization.IncidentReference,
            authorization.AuthorizedBy,
            now));
        store.SetRecovery(BrandingProjectionRecovery.Ready);
        await Settle(reseed, BrandingInboxOutcome.Applied, "reseed", ct).ConfigureAwait(false);
    }

    private async Task Settle(
        BrandingSettingsChangedIntegrationEvent integrationEvent,
        BrandingInboxOutcome outcome,
        string? reasonCode,
        CancellationToken ct)
    {
        store.Record(new BrandingInboxEntry(
            integrationEvent.EventId,
            integrationEvent.SourceEpoch,
            integrationEvent.SourceRevision,
            outcome,
            reasonCode,
            timeProvider.GetUtcNow()));
        await store.Save(ct).ConfigureAwait(false);
    }
}
