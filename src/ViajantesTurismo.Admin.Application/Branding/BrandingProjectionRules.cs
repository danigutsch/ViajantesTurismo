using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>Orders Branding revisions for the Admin projection (ADR-040).</summary>
public static class BrandingProjectionRules
{
    /// <summary>Decides how an incoming revision affects the current projection.</summary>
    /// <param name="current">The applied projection, or <see langword="null" /> before the first revision.</param>
    /// <param name="incoming">The delivered Branding revision event.</param>
    /// <returns>The projection decision.</returns>
    public static BrandingProjectionDecision Decide(
        BrandingProjectionState? current,
        BrandingSettingsChangedIntegrationEvent incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        if (incoming.SourceEpoch == Guid.Empty || incoming.SourceRevision <= 0)
        {
            return BrandingProjectionDecision.InvalidEnvelope;
        }

        if (current is null)
        {
            return BrandingProjectionDecision.Apply;
        }

        if (incoming.SourceEpoch != current.SourceEpoch)
        {
            return BrandingProjectionDecision.EpochMismatch;
        }

        if (incoming.SourceRevision > current.SourceRevision)
        {
            return BrandingProjectionDecision.Apply;
        }

        if (incoming.SourceRevision < current.SourceRevision)
        {
            return BrandingProjectionDecision.Stale;
        }

        return BrandingProjectionState.From(incoming) == current
            ? BrandingProjectionDecision.AlreadyApplied
            : BrandingProjectionDecision.RevisionConflict;
    }
}
