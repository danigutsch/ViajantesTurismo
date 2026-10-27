namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Carries captured branding values to the travel-contract aggregate.
/// </summary>
/// <param name="Version">Content hash identifying this branding appearance.</param>
/// <param name="BrandName">Brand display name.</param>
/// <param name="LogoUri">Root-relative or HTTPS logo address, or null when there is no usable logo.</param>
/// <param name="PrimaryColor">Primary brand color.</param>
/// <param name="AccentColor">Accent brand color.</param>
/// <param name="BackgroundColor">Background color.</param>
/// <param name="TextColor">Text color.</param>
/// <param name="HeadingFontFamily">Font family for headings.</param>
/// <param name="BodyFontFamily">Font family for body text.</param>
/// <param name="FooterText">Footer text shown on the document.</param>
/// <param name="SourceRevision">Branding settings revision the snapshot was captured from.</param>
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
    string FooterText,
    long SourceRevision);
