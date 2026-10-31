namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>An immutable full-state Branding settings revision.</summary>
internal sealed class BrandingSettingsRevisionRecord
{
    private BrandingSettingsRevisionRecord(
        Guid id,
        Guid sourceEpoch,
        long sourceRevision,
        string brandName,
        string primaryColor,
        string accentColor,
        string backgroundColor,
        string textColor,
        string headingFontFamily,
        string bodyFontFamily,
        string? logoUri,
        string? logoSha256,
        DateTimeOffset committedAt)
    {
        Id = id;
        SourceEpoch = sourceEpoch;
        SourceRevision = sourceRevision;
        BrandName = brandName;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
        HeadingFontFamily = headingFontFamily;
        BodyFontFamily = bodyFontFamily;
        LogoUri = logoUri;
        LogoSha256 = logoSha256;
        CommittedAt = committedAt;
    }

    /// <summary>Unique identifier of the revision.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Source epoch the revision belongs to.</summary>
    public Guid SourceEpoch
    {
        get; private init;
    }

    /// <summary>Monotonic revision number within the source epoch.</summary>
    public long SourceRevision
    {
        get; private init;
    }

    /// <summary>Display name of the brand at this revision.</summary>
    public string BrandName
    {
        get; private init;
    }

    /// <summary>Primary brand color as a CSS color.</summary>
    public string PrimaryColor
    {
        get; private init;
    }

    /// <summary>Accent brand color as a CSS color.</summary>
    public string AccentColor
    {
        get; private init;
    }

    /// <summary>Background color as a CSS color.</summary>
    public string BackgroundColor
    {
        get; private init;
    }

    /// <summary>Text color as a CSS color.</summary>
    public string TextColor
    {
        get; private init;
    }

    /// <summary>Font family used for headings.</summary>
    public string HeadingFontFamily
    {
        get; private init;
    }

    /// <summary>Font family used for body text.</summary>
    public string BodyFontFamily
    {
        get; private init;
    }

    /// <summary>Logo URI, or null when no logo was set.</summary>
    public string? LogoUri
    {
        get; private init;
    }

    /// <summary>Content address of the logo at this revision, or null when no logo was set.</summary>
    public string? LogoSha256
    {
        get; private init;
    }

    /// <summary>When the revision was committed.</summary>
    public DateTimeOffset CommittedAt
    {
        get; private init;
    }

    /// <summary>Captures the current settings record as an immutable revision committed at the given time.</summary>
    public static BrandingSettingsRevisionRecord Capture(BrandingSettingsRecord current, DateTimeOffset committedAt)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new BrandingSettingsRevisionRecord(
            Guid.CreateVersion7(),
            current.SourceEpoch,
            current.SourceRevision,
            current.BrandName,
            current.PrimaryColor,
            current.AccentColor,
            current.BackgroundColor,
            current.TextColor,
            current.HeadingFontFamily,
            current.BodyFontFamily,
            current.LogoUri,
            current.LogoSha256,
            committedAt);
    }
}
