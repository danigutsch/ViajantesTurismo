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

    /// <summary>Logo URI, or null when no logo is set.</summary>
    public string? LogoUri
    {
        get; private set;
    }

    /// <summary>Gets the authoritative source lineage; empty until revisions start.</summary>
    public Guid SourceEpoch
    {
        get; private set;
    }

    /// <summary>Gets the monotonic revision within the source epoch; zero until revisions start.</summary>
    public long SourceRevision
    {
        get; private set;
    }

    /// <summary>Whether revisions have started for this row.</summary>
    public bool IsRevisioned => SourceRevision > 0;

    /// <summary>Creates a revisioned record from settings, starting at revision 1 of the given epoch.</summary>
    public static BrandingSettingsRecord StartLineage(BrandingSettings settings, Guid sourceEpoch)
    {
        var record = FromUnrevisionedSettings(settings);
        record.StartRevisions(sourceEpoch);
        return record;
    }

    /// <summary>Creates a record from settings that has not started revisions.</summary>
    public static BrandingSettingsRecord FromUnrevisionedSettings(BrandingSettings settings)
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

    /// <summary>Converts the record to validated settings, or null when persisted values are invalid.</summary>
    public BrandingSettings? ToSettings(IReadOnlyCollection<string> allowedFonts)
    {
        var result = BrandingSettings.Create(ToDto(), allowedFonts);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Starts revisions at revision 1 of the given epoch; throws if already started.</summary>
    public void StartRevisions(Guid sourceEpoch)
    {
        if (IsRevisioned)
        {
            throw new InvalidOperationException("Branding settings revisions have already started.");
        }

        SourceEpoch = sourceEpoch;
        SourceRevision = 1;
    }

    /// <summary>Moves to a new non-empty source epoch and resets the revision to 1.</summary>
    public void RotateEpoch(Guid sourceEpoch)
    {
        if (!IsRevisioned)
        {
            throw new InvalidOperationException("Branding settings revisions have not started; run the Branding bootstrap first.");
        }

        if (sourceEpoch == Guid.Empty || sourceEpoch == SourceEpoch)
        {
            throw new ArgumentException("A rotated epoch must be new and non-empty.", nameof(sourceEpoch));
        }

        SourceEpoch = sourceEpoch;
        SourceRevision = 1;
    }

    /// <summary>Returns whether the record already holds the same values as the given settings.</summary>
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

    /// <summary>Replaces the values with the given settings and increments the source revision.</summary>
    public void ApplyRevision(BrandingSettings settings)
    {
        if (!IsRevisioned)
        {
            throw new InvalidOperationException("Branding settings revisions have not started; run the Branding bootstrap first.");
        }

        ReplaceWith(settings);
        SourceRevision++;
    }

    private void ReplaceWith(BrandingSettings settings)
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
