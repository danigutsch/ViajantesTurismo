using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Provides the Branding settings used before staff save their own.</summary>
public static class BrandingDefaults
{
    /// <summary>Gets the font families Branding settings may use.</summary>
    public static readonly IReadOnlyCollection<string> AllowedFonts = BrandingFontFamilies.All;

    /// <summary>Creates the validated default Branding settings.</summary>
    /// <returns>The default settings.</returns>
    public static BrandingSettings CreateSettings()
    {
        var result = BrandingSettings.Create(CreateDto(), AllowedFonts);
        if (result.TryGetValue(out var settings))
        {
            return settings;
        }

        var validationErrors = result.ErrorDetails?.ValidationErrors;
        var validationMessage = validationErrors is null
            ? result.ErrorDetails?.Detail
            : string.Join(
                "; ",
                validationErrors.SelectMany(static entry => entry.Value.Select(message => $"{entry.Key}: {message}")));

        throw new InvalidOperationException($"Default branding settings are invalid: {validationMessage}");
    }

    /// <summary>Creates the default Branding settings document.</summary>
    /// <returns>The default settings document.</returns>
    public static BrandingSettingsDto CreateDto()
    {
        return new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "#0F766E",
            AccentColor = "#F97316",
            BackgroundColor = "#FFFBF5",
            TextColor = "#1F2937",
            HeadingFontFamily = BrandingFontFamilies.DefaultHeading,
            BodyFontFamily = BrandingFontFamilies.DefaultBody,
            LogoUri = null
        };
    }
}
