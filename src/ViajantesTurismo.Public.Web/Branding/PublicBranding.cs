using SharedKernel.Blazor.Site;
using SharedKernel.Blazor.Site.Theming;
using SharedKernel.Branding;
using ViajantesTurismo.Branding.Contracts.Http;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Public.Web.Branding;

/// <summary>
/// Loads the public Branding settings with safe fallbacks and turns them into the <c>PandaBear.SharedKernel.Blazor.Site</c> theme
/// that <c>/theme.css</c> renders.
/// </summary>
internal static class PublicBranding
{
    /// <summary>The brand name used when the settings have none.</summary>
    public const string DefaultBrandName = "Viajantes Turismo";

    private const string DefaultPrimaryColor = "#0F766E";
    private const string DefaultAccentColor = "#C2410C";
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

    /// <summary>
    /// Maps the settings onto the site theme with <see cref="BrandingSiteTheme" />. A font outside the allowed families falls back to
    /// the default one; colours that are unreadable or break a theme rule, such as text contrast, give the default theme.
    /// </summary>
    public static SiteTheme Theme(BrandingSettingsDto branding)
    {
        ArgumentNullException.ThrowIfNull(branding);

        var settings = new BrandingSettingsDto
        {
            BrandName = branding.BrandName,
            LogoUri = branding.LogoUri,
            PrimaryColor = branding.PrimaryColor,
            AccentColor = branding.AccentColor,
            BackgroundColor = branding.BackgroundColor,
            TextColor = branding.TextColor,
            HeadingFontFamily = KnownFont(branding.HeadingFontFamily, BrandingFontFamilies.DefaultHeading),
            BodyFontFamily = KnownFont(branding.BodyFontFamily, BrandingFontFamilies.DefaultBody)
        };
        var theme = BrandingSiteTheme.Create(settings);
        return theme.IsSuccess ? theme.Value : DefaultTheme.Value;
    }

    private static readonly Lazy<SiteTheme> DefaultTheme = new(() => BrandingSiteTheme.Create(Defaults()).Value);

    private static string KnownFont(string? fontFamily, string fallback) =>
        BrandingSiteTheme.FontFallback(fontFamily) is null ? fallback : fontFamily!;
}
