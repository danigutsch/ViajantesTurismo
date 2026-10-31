namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Valid Branding settings fixtures for infrastructure tests.</summary>
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
        LogoUri = null
    };

    /// <summary>Creates valid domain settings with the given brand name.</summary>
    public static BrandingSettings CreateSettings(string brandName = "Viajantes") =>
        BrandingSettings.Create(CreateDto(brandName), Branding.Infrastructure.BrandingDefaults.AllowedFonts).Value;
}
