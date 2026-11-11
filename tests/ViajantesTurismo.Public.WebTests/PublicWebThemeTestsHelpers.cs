namespace ViajantesTurismo.Public.WebTests;

/// <summary>Builders for the Branding settings used by public theme tests.</summary>
internal static class PublicWebThemeTestsHelpers
{
    /// <summary>Builds the settings of the "Camino Riders" brand, replacing the given values.</summary>
    public static BrandingSettingsDto CreateBranding(
        string? logoUri = null,
        string primaryColor = "#112233",
        string headingFont = "Inter",
        string bodyFont = "Verdana")
    {
        return new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = logoUri,
            PrimaryColor = primaryColor,
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = headingFont,
            BodyFontFamily = bodyFont
        };
    }
}
