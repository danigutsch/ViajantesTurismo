namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class BrandingSettingsTestData
{
    public static BrandingSettingsDto CreateDto(string brandName = "Viajantes") => new()
    {
        BrandName = brandName,
        PrimaryColor = "#112233",
        AccentColor = "#445566",
        BackgroundColor = "#FFFFFF",
        TextColor = "#000000",
        HeadingFontFamily = "Inter",
        BodyFontFamily = "Verdana",
        LogoUri = "https://cdn.example.test/logo.svg"
    };

    public static BrandingSettings CreateSettings(string brandName = "Viajantes") =>
        BrandingSettings.Create(CreateDto(brandName), Branding.Infrastructure.BrandingDefaults.AllowedFonts).Value;
}
