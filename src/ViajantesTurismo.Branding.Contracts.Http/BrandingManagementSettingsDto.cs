namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>The Branding settings document returned to management editors with its source revision.</summary>
public sealed class BrandingManagementSettingsDto
{
    /// <summary>Gets the display brand name.</summary>
    public string BrandName { get; init; } = string.Empty;

    /// <summary>Gets the primary CSS color.</summary>
    public string PrimaryColor { get; init; } = string.Empty;

    /// <summary>Gets the accent CSS color.</summary>
    public string AccentColor { get; init; } = string.Empty;

    /// <summary>Gets the background CSS color.</summary>
    public string BackgroundColor { get; init; } = string.Empty;

    /// <summary>Gets the text CSS color.</summary>
    public string TextColor { get; init; } = string.Empty;

    /// <summary>Gets the heading font family.</summary>
    public string HeadingFontFamily { get; init; } = string.Empty;

    /// <summary>Gets the body font family.</summary>
    public string BodyFontFamily { get; init; } = string.Empty;

    /// <summary>Gets the optional public logo URI.</summary>
    public Uri? LogoUri
    {
        get; init;
    }

    /// <summary>Gets the source lineage epoch.</summary>
    public Guid SourceEpoch
    {
        get; init;
    }

    /// <summary>Gets the positive source revision within the epoch.</summary>
    public long SourceRevision
    {
        get; init;
    }
}
