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
        CommittedAt = committedAt;
    }

    public Guid Id
    {
        get; private init;
    }

    public Guid SourceEpoch
    {
        get; private init;
    }

    public long SourceRevision
    {
        get; private init;
    }

    public string BrandName
    {
        get; private init;
    }

    public string PrimaryColor
    {
        get; private init;
    }

    public string AccentColor
    {
        get; private init;
    }

    public string BackgroundColor
    {
        get; private init;
    }

    public string TextColor
    {
        get; private init;
    }

    public string HeadingFontFamily
    {
        get; private init;
    }

    public string BodyFontFamily
    {
        get; private init;
    }

    public string? LogoUri
    {
        get; private init;
    }

    public DateTimeOffset CommittedAt
    {
        get; private init;
    }

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
            committedAt);
    }
}
