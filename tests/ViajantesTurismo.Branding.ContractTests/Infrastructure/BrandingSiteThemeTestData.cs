using SharedKernel.Branding;

namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>Builds Branding settings for the site theme mapping tests.</summary>
internal static class BrandingSiteThemeTestData
{
    /// <summary>Creates settings whose colours pass every contrast rule.</summary>
    public static BrandingSettingsDto CreateSettings() => new()
    {
        BrandName = "Viajantes",
        PrimaryColor = "#112233",
        AccentColor = "#445566",
        BackgroundColor = "#FFFFFF",
        TextColor = "#000000",
        HeadingFontFamily = "Georgia",
        BodyFontFamily = "system-ui"
    };
}
