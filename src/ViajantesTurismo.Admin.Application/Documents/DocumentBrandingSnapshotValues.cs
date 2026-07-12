namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Carries captured branding values to the travel-contract aggregate.
/// </summary>
internal sealed record DocumentBrandingSnapshotValues(
    string Version,
    string BrandName,
    Uri? LogoUri,
    string PrimaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string HeadingFontFamily,
    string BodyFontFamily,
    string FooterText);
