namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Editable public website theme settings.
/// </summary>
public sealed class PublicThemeSettingsDto
{
    /// <summary>
    /// Gets or sets the primary brand color as a hex CSS color.
    /// </summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accent color as a hex CSS color.
    /// </summary>
    public string AccentColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page background color as a hex CSS color.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body text color as a hex CSS color.
    /// </summary>
    public string TextColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the heading font family.
    /// </summary>
    public string HeadingFontFamily { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body font family.
    /// </summary>
    public string BodyFontFamily { get; set; } = string.Empty;
}
