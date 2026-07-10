using SharedKernel.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class BrandingSettingsRecord
{
    public static readonly Guid SettingsId = Guid.Parse("f14d4ca3-7327-47b2-a6a7-e126a5598f45");

    internal BrandingSettingsRecord(
        Guid id,
        string brandName,
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily,
        string? logoUri)
    {
        Id = id;
        BrandName = brandName;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
        HeadingFontFamily = headingFontFamily;
        BodyFontFamily = bodyFontFamily;
        LogoUri = logoUri;
    }

    public Guid Id { get; private set; }

    public string BrandName { get; private set; }

    public string PrimaryColor { get; private set; }

    public string AccentColor { get; private set; }

    public string BackgroundColor { get; private set; }

    public string TextColor { get; private set; }

    public string HeadingFontFamily { get; private set; }

    public string BodyFontFamily { get; private set; }

    public string? LogoUri { get; private set; }

    public static BrandingSettingsRecord FromSettings(BrandingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new BrandingSettingsRecord(
            SettingsId,
            settings.BrandName,
            settings.PrimaryColor,
            settings.AccentColor,
            settings.BackgroundColor,
            settings.TextColor,
            settings.HeadingFontFamily,
            settings.BodyFontFamily,
            settings.LogoUri);
    }

    public BrandingSettings? ToSettings(IReadOnlyCollection<string> allowedFonts)
    {
        var result = BrandingSettings.Create(ToDto(), allowedFonts);
        return result.IsSuccess ? result.Value : null;
    }

    public void ReplaceWith(BrandingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        BrandName = settings.BrandName;
        PrimaryColor = settings.PrimaryColor;
        AccentColor = settings.AccentColor;
        BackgroundColor = settings.BackgroundColor;
        TextColor = settings.TextColor;
        HeadingFontFamily = settings.HeadingFontFamily;
        BodyFontFamily = settings.BodyFontFamily;
        LogoUri = settings.LogoUri;
    }

    private BrandingSettingsDto ToDto()
    {
        return new BrandingSettingsDto
        {
            BrandName = BrandName,
            PrimaryColor = PrimaryColor,
            AccentColor = AccentColor,
            BackgroundColor = BackgroundColor,
            TextColor = TextColor,
            HeadingFontFamily = HeadingFontFamily,
            BodyFontFamily = BodyFontFamily,
            LogoUri = string.IsNullOrWhiteSpace(LogoUri) ? null : LogoUri
        };
    }
}
