using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>The current Branding settings together with the source revision that identifies them.</summary>
/// <param name="Settings">The settings values.</param>
/// <param name="SourceEpoch">The source lineage epoch.</param>
/// <param name="SourceRevision">The positive source revision within the epoch.</param>
public sealed record BrandingSettingsSnapshot(BrandingSettings Settings, Guid SourceEpoch, long SourceRevision);
