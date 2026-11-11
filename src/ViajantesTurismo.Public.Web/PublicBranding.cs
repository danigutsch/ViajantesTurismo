using System.Globalization;
using System.Text;
using PandaBear.Blazor.Site;
using PandaBear.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Public.Web;

/// <summary>
/// Loads the public Branding settings with safe fallbacks and turns them into the site stylesheet that overrides the
/// <c>PandaBear.SharedKernel.Blazor.Site</c> design tokens. Only validated values reach the stylesheet: a colour must be
/// <c>#RRGGBB</c> and a font must be on the allow-list, otherwise the default is used.
/// </summary>
internal static class PublicBranding
{
    /// <summary>The brand name used when the settings have none.</summary>
    public const string DefaultBrandName = "Viajantes Turismo";

    private const string DefaultPrimaryColor = "#0F766E";
    private const string DefaultAccentColor = "#F97316";
    private const string DefaultBackgroundColor = "#FFFBF5";
    private const string DefaultTextColor = "#1F2937";

    /// <summary>Loads the public settings, or the defaults when Branding is unavailable or answers with an unreadable body.</summary>
    public static async Task<BrandingSettingsDto> Load(IBrandingApiClient brandingApi, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(brandingApi);

        try
        {
            return await brandingApi.GetPublicSettings(ct);
        }
        catch (HttpRequestException)
        {
            return Defaults();
        }
        catch (InvalidOperationException)
        {
            return Defaults();
        }
        catch (System.Text.Json.JsonException)
        {
            return Defaults();
        }
        catch (NotSupportedException)
        {
            return Defaults();
        }
    }

    /// <summary>Gets the default settings.</summary>
    public static BrandingSettingsDto Defaults()
    {
        return new BrandingSettingsDto
        {
            BrandName = DefaultBrandName,
            LogoUri = string.Empty,
            PrimaryColor = DefaultPrimaryColor,
            AccentColor = DefaultAccentColor,
            BackgroundColor = DefaultBackgroundColor,
            TextColor = DefaultTextColor,
            HeadingFontFamily = BrandingFontFamilies.DefaultHeading,
            BodyFontFamily = BrandingFontFamilies.DefaultBody
        };
    }

    /// <summary>Gets the brand name, or the default when it is blank.</summary>
    public static string BrandName(BrandingSettingsDto? branding) =>
        string.IsNullOrWhiteSpace(branding?.BrandName) ? DefaultBrandName : branding.BrandName.Trim();

    /// <summary>Gets the logo address when it is a local, root-relative address; anything else is not rendered.</summary>
    public static LinkTarget? Logo(BrandingSettingsDto? branding)
    {
        var value = branding?.LogoUri?.Trim();
        if (string.IsNullOrEmpty(value) || value[0] != '/')
        {
            return null;
        }

        var target = LinkTarget.Create(value);
        return target.IsSuccess ? target.Value : null;
    }

    /// <summary>Builds the stylesheet that overrides the site tokens with the brand colours and fonts.</summary>
    /// <remarks>
    /// The brand colours are chosen for a light page, so each is the light side of <c>light-dark()</c>; the dark side mixes
    /// the brand colour with white, or keeps the package's dark surface and text, so the dark scheme stays readable.
    /// </remarks>
    public static string Stylesheet(BrandingSettingsDto branding)
    {
        ArgumentNullException.ThrowIfNull(branding);

        var primary = SafeColor(branding.PrimaryColor, DefaultPrimaryColor);
        var accent = SafeColor(branding.AccentColor, DefaultAccentColor);
        var background = SafeColor(branding.BackgroundColor, DefaultBackgroundColor);
        var text = SafeColor(branding.TextColor, DefaultTextColor);
        var headingFont = SafeFont(branding.HeadingFontFamily, BrandingFontFamilies.DefaultHeading);
        var bodyFont = SafeFont(branding.BodyFontFamily, BrandingFontFamilies.DefaultBody);

        var css = new StringBuilder();
        css.AppendLine(":root {");
        Token(css, "--site-color-primary", $"light-dark({primary}, {Tint(primary, 55)})");
        Token(css, "--site-color-primary-hover", $"light-dark({Shade(primary, 80)}, {Tint(primary, 40)})");
        Token(css, "--site-color-focus", $"light-dark({primary}, {Tint(primary, 55)})");
        Token(css, "--site-color-accent", $"light-dark({accent}, {Tint(accent, 55)})");
        Token(css, "--site-color-accent-hover", $"light-dark({Shade(accent, 80)}, {Tint(accent, 40)})");
        Token(css, "--site-color-surface", $"light-dark({background}, #111827)");
        Token(css, "--site-color-text", $"light-dark({text}, #f3f4f6)");
        Token(css, "--site-font-heading", headingFont);
        Token(css, "--site-font-body", bodyFont);
        css.AppendLine("}");
        css.AppendLine();
        css.AppendLine(".public-brand { display: inline-flex; align-items: center; gap: var(--site-space-2); color: inherit; text-decoration: none; }");
        css.AppendLine(".public-brand__logo { block-size: 2.5rem; inline-size: auto; }");
        css.AppendLine(".public-brand__mark { display: inline-grid; place-items: center; block-size: 2.5rem; inline-size: 2.5rem; border-radius: var(--site-radius); background: var(--site-color-primary); color: var(--site-color-on-primary); font-size: var(--site-font-size-sm); }");
        css.AppendLine(".tour-hero { width: min(100% - 2 * var(--site-space-3), var(--site-content-width)); margin-inline: auto; }");
        css.AppendLine(".tour-gallery { display: grid; gap: var(--site-space-3); grid-template-columns: repeat(auto-fit, minmax(min(18rem, 100%), 1fr)); }");
        css.AppendLine(".tour-gallery figure { margin: 0; }");
        return css.ToString();
    }

    private static void Token(StringBuilder css, string name, string value) =>
        css.Append(CultureInfo.InvariantCulture, $"    {name}: {value};").AppendLine();

    private static string Tint(string color, int percent) =>
        string.Create(CultureInfo.InvariantCulture, $"color-mix(in srgb, {color} {percent}%, white)");

    private static string Shade(string color, int percent) =>
        string.Create(CultureInfo.InvariantCulture, $"color-mix(in srgb, {color} {percent}%, black)");

    private static string SafeColor(string? color, string fallback)
    {
        var value = color?.Trim().ToUpperInvariant();
        return value is { Length: 7 } && value[0] == '#' && value[1..].All(Uri.IsHexDigit)
            ? value
            : fallback;
    }

    private static string SafeFont(string? fontFamily, string fallback)
    {
        var value = fontFamily?.Trim();
        return value is null
            ? fallback
            : BrandingFontFamilies.All.FirstOrDefault(font => string.Equals(font, value, StringComparison.OrdinalIgnoreCase)) ?? fallback;
    }
}
