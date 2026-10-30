using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

/// <summary>Builds Branding settings test data for PostgreSQL integration tests.</summary>
internal static class BrandingPostgreSqlTestData
{
    /// <summary>Creates valid Branding settings with the given brand name and fixed colors and fonts.</summary>
    public static BrandingSettings CreateSettings(string brandName) => BrandingSettings.Create(new BrandingSettingsDto
    {
        BrandName = brandName,
        PrimaryColor = "#0F766E",
        AccentColor = "#F97316",
        BackgroundColor = "#FFFBF5",
        TextColor = "#1F2937",
        HeadingFontFamily = "Georgia",
        BodyFontFamily = "system-ui",
    }, BrandingFontFamilies.All).Value;
}
