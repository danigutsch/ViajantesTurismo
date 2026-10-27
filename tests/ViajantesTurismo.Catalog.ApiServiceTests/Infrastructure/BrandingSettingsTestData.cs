namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Builds valid Branding settings fixtures.</summary>
internal static class BrandingSettingsTestData
{
    /// <summary>Creates a valid settings DTO with the given brand name.</summary>
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

    /// <summary>Creates valid domain settings from the DTO using the default allowed fonts.</summary>
    public static BrandingSettings CreateSettings(string brandName = "Viajantes") =>
        BrandingSettings.Create(CreateDto(brandName), ViajantesTurismo.Branding.Infrastructure.BrandingDefaults.AllowedFonts).Value;
}
