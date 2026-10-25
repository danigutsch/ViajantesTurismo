using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Branding;

internal static class BrandingPostgreSqlTestData
{
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
