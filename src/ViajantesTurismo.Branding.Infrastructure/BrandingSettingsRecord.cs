using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Persisted current Branding settings row, with its source epoch and revision.</summary>
internal sealed class BrandingSettingsRecord
{
    /// <summary>Fixed identifier of the single Branding settings row.</summary>
    public static readonly Guid SettingsId = Guid.Parse("f14d4ca3-7327-47b2-a6a7-e126a5598f45");

    /// <summary>Initializes a new instance of the <see cref="BrandingSettingsRecord"/> class.</summary>
    internal BrandingSettingsRecord(
        Guid id,
        string brandName,
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily,
        string? logoUri,
        string? logoSha256)
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
        LogoSha256 = logoSha256;
    }

    /// <summary>Identifier of the settings row.</summary>
    public Guid Id
    {
        get; private set;
    }

    /// <summary>Display name of the brand.</summary>
    public string BrandName
    {
        get; private set;
    }

    /// <summary>Primary brand color as a CSS color.</summary>
    public string PrimaryColor
    {
        get; private set;
    }

    /// <summary>Accent brand color as a CSS color.</summary>
    public string AccentColor
    {
        get; private set;
    }

    /// <summary>Background color as a CSS color.</summary>
    public string BackgroundColor
    {
        get; private set;
    }

    /// <summary>Text color as a CSS color.</summary>
    public string TextColor
    {
        get; private set;
    }

    /// <summary>Font family used for headings.</summary>
    public string HeadingFontFamily
    {
        get; private set;
    }

    /// <summary>Font family used for body text.</summary>
    public string BodyFontFamily
    {
        get; private set;
    }

    /// <summary>App-generated root-relative URL of the logo, or null when no logo is set.</summary>
    public string? LogoUri
    {
        get; private set;
    }

    /// <summary>Content address of the stored logo, or null when no logo is set.</summary>
    public string? LogoSha256
    {
        get; private set;
    }

    /// <summary>Gets the authoritative source lineage.</summary>
    public Guid SourceEpoch
    {
        get; private set;
    }

    /// <summary>Gets the monotonic positive revision within the source epoch.</summary>
    public long SourceRevision
    {
        get; private set;
    }

    /// <summary>Creates a record from settings, starting at revision 1 of the given epoch.</summary>
    public static BrandingSettingsRecord StartLineage(BrandingSettings settings, Guid sourceEpoch)
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
            null,
            null)
        {
            SourceEpoch = sourceEpoch,
            SourceRevision = 1
        };
    }

    /// <summary>Converts the record to validated settings, or null when persisted values are invalid.</summary>
    public BrandingSettings? ToSettings(IReadOnlyCollection<string> allowedFonts)
    {
        var result = BrandingSettings.Create(ToDto(), allowedFonts);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Moves to a new non-empty source epoch and resets the revision to 1.</summary>
    public void RotateEpoch(Guid sourceEpoch)
    {
        if (sourceEpoch == Guid.Empty || sourceEpoch == SourceEpoch)
        {
            throw new ArgumentException("A rotated epoch must be new and non-empty.", nameof(sourceEpoch));
        }

        SourceEpoch = sourceEpoch;
        SourceRevision = 1;
    }

    /// <summary>
    /// Returns whether the record already holds the same values as the given settings, whose logo URI is either
    /// absent (no logo) or the record's own generated logo URL (keep the logo).
    /// </summary>
    public bool HasSameValues(BrandingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return BrandName == settings.BrandName
            && PrimaryColor == settings.PrimaryColor
            && AccentColor == settings.AccentColor
            && BackgroundColor == settings.BackgroundColor
            && TextColor == settings.TextColor
            && HeadingFontFamily == settings.HeadingFontFamily
            && BodyFontFamily == settings.BodyFontFamily
            && LogoUri == settings.LogoUri;
    }

    /// <summary>Returns whether the record already holds the given logo address.</summary>
    public bool HasSameLogo(string? logoSha256) => LogoSha256 == logoSha256;

    /// <summary>Returns whether the given settings keep or clear the logo this record holds, and never name another.</summary>
    public bool AcceptsLogoUri(string? logoUri) => logoUri is null || logoUri == LogoUri;

    /// <summary>Replaces the values with the given settings, keeping the logo they keep, and increments the source revision.</summary>
    public void ApplyRevision(BrandingSettings settings)
    {
        if (!AcceptsLogoUri(settings.LogoUri))
        {
            throw new ArgumentException("Branding settings may only keep or clear the stored logo.", nameof(settings));
        }

        ReplaceWith(settings, settings.LogoUri is null ? null : LogoSha256);
        SourceRevision++;
    }

    /// <summary>Points the settings at another logo, or clears it, and increments the source revision.</summary>
    public void ApplyLogoRevision(BrandingSettings settings, string? logoSha256)
    {
        ReplaceWith(settings, logoSha256);
        SourceRevision++;
    }

    private void ReplaceWith(BrandingSettings settings, string? logoSha256)
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
        LogoSha256 = logoSha256;
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
