namespace ViajantesTurismo.Catalog.Domain.PublicTheme;

/// <summary>
/// Defines canonical default and allow-list values for public website theme settings.
/// </summary>
public static class PublicThemeSettingsDefaults
{
    /// <summary>
    /// Gets the default primary brand color.
    /// </summary>
    public const string PrimaryColor = "#0F766E";

    /// <summary>
    /// Gets the default accent color.
    /// </summary>
    public const string AccentColor = "#F97316";

    /// <summary>
    /// Gets the default page background color.
    /// </summary>
    public const string BackgroundColor = "#FFFBF5";

    /// <summary>
    /// Gets the default body text color.
    /// </summary>
    public const string TextColor = "#1F2937";

    /// <summary>
    /// Gets the default heading font family.
    /// </summary>
    public const string HeadingFontFamily = "Georgia";

    /// <summary>
    /// Gets the default body font family.
    /// </summary>
    public const string BodyFontFamily = "system-ui";

    /// <summary>
    /// Gets the allowed public theme font families in their canonical CSS output form.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedFontFamilies = Array.AsReadOnly(["Arial", "Georgia", "Inter", "system-ui", "Verdana"]);
}
