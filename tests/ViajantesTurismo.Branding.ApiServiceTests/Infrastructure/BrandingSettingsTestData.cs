namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal static class BrandingSettingsTestData
{
    public static BrandingSettingsDto CreateDto() => new()
    {
        BrandName = "Viajantes",
        PrimaryColor = "#112233",
        AccentColor = "#445566",
        BackgroundColor = "#FFFFFF",
        TextColor = "#000000",
        HeadingFontFamily = "Inter",
        BodyFontFamily = "Verdana",
        LogoUri = "https://cdn.example.test/logo.svg"
    };
}
