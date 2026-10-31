using System.Security.Cryptography;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>Orders Branding revisions for the Admin projection (ADR-040).</summary>
public static class BrandingProjectionRules
{
    private const int MaxLogoBytes = 40 * 1024;

    private const string LogoContentType = "image/webp";

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

        if (!HasValidLogo(incoming))
        {
            return BrandingProjectionDecision.InvalidLogo;
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

    /// <summary>Checks that the event carries either no logo or a complete, hash-matching WebP logo within the size cap.</summary>
    /// <param name="incoming">The delivered Branding revision event.</param>
    /// <returns><see langword="true" /> when the logo fields are absent or valid.</returns>
    public static bool HasValidLogo(BrandingSettingsChangedIntegrationEvent incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        if (incoming.LogoSha256 is null && incoming.LogoContentType is null && incoming.LogoContent is null)
        {
            return true;
        }

        return incoming is { LogoSha256: { } sha256, LogoContentType: LogoContentType, LogoContent: { Length: > 0 and <= MaxLogoBytes } content }
            && string.Equals(sha256, Convert.ToHexStringLower(SHA256.HashData(content.Span)), StringComparison.Ordinal);
    }
}
