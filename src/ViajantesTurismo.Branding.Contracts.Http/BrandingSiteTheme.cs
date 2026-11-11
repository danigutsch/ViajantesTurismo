using PandaBear.Blazor.Site.Theming;
using PandaBear.Branding;
using PandaBear.Results;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>
/// Maps Branding settings onto the SharedKernel site theme. The primary, accent, background and text colours are the light values;
/// their dark values and every other colour come from <see cref="SiteTheme.Default" />, and the hover colours are derived. The heading
/// font sets h1 to h6 and the body font sets the body, each as a generic system stack: Georgia is serif, the other allowed families are
/// sans-serif. Errors are reported on the <see cref="BrandingSettingsDto" /> property names.
/// </summary>
public static class BrandingSiteTheme
{
    private const string InvalidDetail = "Branding settings are invalid.";

    private static readonly TypographyRole[] Headings =
    [
        TypographyRole.H1, TypographyRole.H2, TypographyRole.H3, TypographyRole.H4, TypographyRole.H5, TypographyRole.H6
    ];

    /// <summary>Creates the theme and enforces every rule of <see cref="SiteTheme.Create" />, such as text contrast.</summary>
    /// <param name="settings">The Branding settings.</param>
    /// <returns>The theme, or the validation errors on the settings fields.</returns>
    public static Result<SiteTheme> Create(BrandingSettingsDto settings) => Build(settings, draft: false);

    /// <summary>Creates the theme for a preview: only the values must be readable, a broken rule is left to the review.</summary>
    /// <param name="settings">The Branding settings.</param>
    /// <returns>The theme, or the validation errors of unreadable colours or fonts on the settings fields.</returns>
    public static Result<SiteTheme> CreateDraft(BrandingSettingsDto settings) => Build(settings, draft: true);

    /// <summary>Gets the generic font family of an allowed Branding font family, compared ignoring case.</summary>
    /// <param name="fontFamily">The font family.</param>
    /// <returns>The generic family, or <see langword="null" /> when the family is not allowed.</returns>
    public static GenericFontFamily? FontFallback(string? fontFamily)
    {
        var value = fontFamily?.Trim();
        if (value is null || !BrandingFontFamilies.All.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.Equals(value, "Georgia", StringComparison.OrdinalIgnoreCase) ? GenericFontFamily.Serif : GenericFontFamily.SansSerif;
    }

    private static Result<SiteTheme> Build(BrandingSettingsDto settings, bool draft)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var primary = Color(settings.PrimaryColor, nameof(BrandingSettingsDto.PrimaryColor), errors);
        var accent = Color(settings.AccentColor, nameof(BrandingSettingsDto.AccentColor), errors);
        var background = Color(settings.BackgroundColor, nameof(BrandingSettingsDto.BackgroundColor), errors);
        var text = Color(settings.TextColor, nameof(BrandingSettingsDto.TextColor), errors);
        var heading = Font(settings.HeadingFontFamily, nameof(BrandingSettingsDto.HeadingFontFamily), errors);
        var body = Font(settings.BodyFontFamily, nameof(BrandingSettingsDto.BodyFontFamily), errors);
        if (primary is null || accent is null || background is null || text is null || heading is null || body is null)
        {
            return Result.Invalid<SiteTheme>(InvalidDetail, errors);
        }

        var defaults = SiteTheme.Default.Colors;
        var colors = SiteColors.Create(
            primary: ThemeColor.Create(primary, defaults.Primary.Dark),
            onPrimary: defaults.OnPrimary,
            accent: ThemeColor.Create(accent, defaults.Accent.Dark),
            onAccent: defaults.OnAccent,
            text: ThemeColor.Create(text, defaults.Text.Dark),
            muted: defaults.Muted,
            surface: ThemeColor.Create(background, defaults.Surface.Dark),
            surfaceAlt: defaults.SurfaceAlt,
            border: defaults.Border,
            focus: defaults.Focus);
        var typography = SiteTypography.Empty.With(TypographyRole.Body, Family(body.Value));
        foreach (var role in Headings)
        {
            typography = typography.With(role, Family(heading.Value));
        }

        var theme = draft ? SiteTheme.CreateDraft(colors, typography) : SiteTheme.Create(colors, typography);
        return theme.IsSuccess ? theme : Result.Invalid<SiteTheme>(InvalidDetail, ToSettingsFields(theme.ErrorDetails.ValidationErrors));
    }

    private static CssColor? Color(string? value, string field, Dictionary<string, string[]> errors)
    {
        var color = CssColor.Create(value?.Trim());
        if (color.IsSuccess)
        {
            return color.Value;
        }

        errors[field] = ["Use a #RRGGBB hex colour."];
        return null;
    }

    private static GenericFontFamily? Font(string? value, string field, Dictionary<string, string[]> errors)
    {
        var fallback = FontFallback(value);
        if (fallback is null)
        {
            errors[field] = [$"Use one of: {string.Join(", ", BrandingFontFamilies.All)}."];
        }

        return fallback;
    }

    private static TypographyStyle Family(GenericFontFamily fallback) => TypographyStyle.Create(fallback: fallback).Value;

    private static Dictionary<string, string[]> ToSettingsFields(IReadOnlyDictionary<string, IReadOnlyList<string>>? themeErrors)
    {
        var fields = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (path, messages) in themeErrors ?? new Dictionary<string, IReadOnlyList<string>>())
        {
            var field = SettingsField(path);
            if (!fields.TryGetValue(field, out var list))
            {
                list = [];
                fields[field] = list;
            }

            list.AddRange(messages);
        }

        return fields.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    // A text pair is reported on its foreground; the Branding field that can repair it is the one the owner edits.
    private static string SettingsField(string path) => path switch
    {
        "colors.primary" or "colors.primaryHover" or "colors.onPrimary" => nameof(BrandingSettingsDto.PrimaryColor),
        "colors.accent" or "colors.accentHover" or "colors.onAccent" => nameof(BrandingSettingsDto.AccentColor),
        "colors.text" => nameof(BrandingSettingsDto.TextColor),
        "colors.muted" or "colors.focus" or "colors.surface" or "colors.surfaceAlt" => nameof(BrandingSettingsDto.BackgroundColor),
        _ when path.StartsWith("typography.h", StringComparison.Ordinal) => nameof(BrandingSettingsDto.HeadingFontFamily),
        _ when path.StartsWith("typography.", StringComparison.Ordinal) => nameof(BrandingSettingsDto.BodyFontFamily),
        _ => path
    };
}
