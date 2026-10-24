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

        var current = await store.GetCurrent(ct).ConfigureAwait(false);
        var decision = BrandingProjectionRules.Decide(current, integrationEvent);
        var (outcome, reasonCode) = decision switch
        {
            BrandingProjectionDecision.Apply => (BrandingInboxOutcome.Applied, (string?)null),
            BrandingProjectionDecision.AlreadyApplied or BrandingProjectionDecision.Stale => (BrandingInboxOutcome.Stale, null),
            BrandingProjectionDecision.RevisionConflict => (BrandingInboxOutcome.Rejected, "revision-conflict"),
            BrandingProjectionDecision.EpochMismatch => (BrandingInboxOutcome.Rejected, "epoch-mismatch"),
            BrandingProjectionDecision.InvalidEnvelope => (BrandingInboxOutcome.Rejected, "invalid-envelope"),
            _ => throw new InvalidOperationException($"Unsupported Branding projection decision '{decision}'.")
        };

        if (decision == BrandingProjectionDecision.Apply)
        {
            store.Replace(BrandingProjectionState.From(integrationEvent));
        }

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
