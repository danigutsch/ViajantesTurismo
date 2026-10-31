using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>The Branding settings revision Admin last applied to its local projection.</summary>
/// <param name="SourceEpoch">The accepted source epoch.</param>
/// <param name="SourceRevision">The applied source revision.</param>
/// <param name="BrandName">The display brand name.</param>
/// <param name="PrimaryColor">The primary CSS color.</param>
/// <param name="AccentColor">The accent CSS color.</param>
/// <param name="BackgroundColor">The background CSS color.</param>
/// <param name="TextColor">The text CSS color.</param>
/// <param name="HeadingFontFamily">The heading font family.</param>
/// <param name="BodyFontFamily">The body font family.</param>
/// <param name="LogoUri">The optional public logo URI.</param>
/// <param name="Logo">The optional locally stored logo bytes.</param>
public sealed record BrandingProjectionState(
    Guid SourceEpoch,
    long SourceRevision,
    string BrandName,
    string PrimaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string HeadingFontFamily,
    string BodyFontFamily,
    Uri? LogoUri,
    BrandingProjectionLogo? Logo = null)
{
    /// <summary>Creates the projection state carried by one Branding revision event.</summary>
    /// <param name="incoming">The Branding revision event.</param>
    /// <returns>The projection state.</returns>
    public static BrandingProjectionState From(BrandingSettingsChangedIntegrationEvent incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        return new BrandingProjectionState(
            incoming.SourceEpoch,
            incoming.SourceRevision,
            incoming.BrandName,
            incoming.PrimaryColor,
            incoming.AccentColor,
            incoming.BackgroundColor,
            incoming.TextColor,
            incoming.HeadingFontFamily,
            incoming.BodyFontFamily,
            incoming.LogoUri,
            incoming.LogoSha256 is { } sha256 && incoming.LogoContentType is { } contentType && incoming.LogoContent is { } content
                ? new BrandingProjectionLogo(sha256, contentType, content)
                : null);
    }
}
