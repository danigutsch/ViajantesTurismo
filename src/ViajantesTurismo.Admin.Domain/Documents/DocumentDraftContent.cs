namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Defines validated source and branding inputs for one document revision.</summary>
/// <param name="TemplateId">The template identifier.</param>
/// <param name="TemplateVersion">The template version.</param>
/// <param name="SourceVersion">The deterministic source-data version.</param>
/// <param name="Fields">The classified document fields.</param>
/// <param name="BrandingVersion">The captured branding version.</param>
/// <param name="BrandingName">The captured brand name.</param>
/// <param name="BrandingLogoUri">The captured brand logo URI.</param>
/// <param name="BrandingPrimaryColor">The captured primary color.</param>
/// <param name="BrandingAccentColor">The captured accent color.</param>
/// <param name="BrandingBackgroundColor">The captured background color.</param>
/// <param name="BrandingTextColor">The captured text color.</param>
/// <param name="BrandingHeadingFontFamily">The captured heading font.</param>
/// <param name="BrandingBodyFontFamily">The captured body font.</param>
/// <param name="BrandingFooterText">The captured footer text.</param>
public sealed record DocumentDraftContent(
    string TemplateId,
    string TemplateVersion,
    string SourceVersion,
    IReadOnlyList<DocumentField> Fields,
    string BrandingVersion,
    string BrandingName,
    Uri? BrandingLogoUri,
    string BrandingPrimaryColor,
    string BrandingAccentColor,
    string BrandingBackgroundColor,
    string BrandingTextColor,
    string BrandingHeadingFontFamily,
    string BrandingBodyFontFamily,
    string BrandingFooterText);
