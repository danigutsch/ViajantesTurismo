namespace ViajantesTurismo.Resources;

/// <summary>
/// Defines the ViajantesTurismo-supported font families for branding settings.
/// </summary>
public static class BrandingFontFamilies
{
    private static readonly string[] AllValues = ["Arial", "Georgia", "Inter", "system-ui", "Verdana"];

    /// <summary>
    /// Gets the default heading font family.
    /// </summary>
    public const string DefaultHeading = "Georgia";

    /// <summary>
    /// Gets the default body font family.
    /// </summary>
    public const string DefaultBody = "system-ui";

    /// <summary>
    /// Gets the allowed font families for ViajantesTurismo branding settings.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(AllValues);
}
