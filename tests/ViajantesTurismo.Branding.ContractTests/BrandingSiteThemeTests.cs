using PandaBear.Blazor.Site.Theming;
using PandaBear.Branding;
using ViajantesTurismo.Branding.ContractTests.Infrastructure;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Branding.ContractTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ContractCategory)]
public sealed class BrandingSiteThemeTests
{
    [Fact]
    public void Brand_colours_become_the_light_values_and_the_rest_comes_from_the_default_theme()
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();

        // Act
        var theme = BrandingSiteTheme.Create(settings).Value;

        // Assert
        var defaults = SiteTheme.Default.Colors;
        theme.Colors.Primary.ShouldBe(ThemeColor.Create(CssColor.FromString("#112233"), defaults.Primary.Dark));
        theme.Colors.Accent.ShouldBe(ThemeColor.Create(CssColor.FromString("#445566"), defaults.Accent.Dark));
        theme.Colors.Surface.ShouldBe(ThemeColor.Create(CssColor.FromString("#ffffff"), defaults.Surface.Dark));
        theme.Colors.Text.ShouldBe(ThemeColor.Create(CssColor.FromString("#000000"), defaults.Text.Dark));
        theme.Colors.OnPrimary.ShouldBe(defaults.OnPrimary);
        theme.Colors.OnAccent.ShouldBe(defaults.OnAccent);
        theme.Colors.Muted.ShouldBe(defaults.Muted);
        theme.Colors.SurfaceAlt.ShouldBe(defaults.SurfaceAlt);
        theme.Colors.Border.ShouldBe(defaults.Border);
        theme.Colors.Focus.ShouldBe(defaults.Focus);
    }

    [Theory]
    [InlineData("Georgia", GenericFontFamily.Serif)]
    [InlineData("georgia", GenericFontFamily.Serif)]
    [InlineData("Inter", GenericFontFamily.SansSerif)]
    [InlineData("Arial", GenericFontFamily.SansSerif)]
    [InlineData("Verdana", GenericFontFamily.SansSerif)]
    [InlineData("system-ui", GenericFontFamily.SansSerif)]
    public void The_heading_font_sets_every_heading_level_and_the_body_font_sets_the_body(string family, GenericFontFamily expected)
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();
        settings.HeadingFontFamily = family;
        settings.BodyFontFamily = family;

        // Act
        var theme = BrandingSiteTheme.Create(settings).Value;

        // Assert
        TypographyRole[] headings = [TypographyRole.H1, TypographyRole.H2, TypographyRole.H3, TypographyRole.H4, TypographyRole.H5, TypographyRole.H6];
        headings.Select(role => theme.Typography[role].Fallback).Distinct().ShouldHaveSingleItem().ShouldBe(expected);
        theme.Typography[TypographyRole.Body].Fallback.ShouldBe(expected);
    }

    [Theory]
    [InlineData(nameof(BrandingSettingsDto.TextColor), "#BBBBBB")]
    [InlineData(nameof(BrandingSettingsDto.PrimaryColor), "#7DD3FC")]
    [InlineData(nameof(BrandingSettingsDto.AccentColor), "#F97316")]
    [InlineData(nameof(BrandingSettingsDto.BackgroundColor), "#6B7280")]
    public void A_colour_that_breaks_a_contrast_rule_is_refused_on_its_field(string field, string color)
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();
        typeof(BrandingSettingsDto).GetProperty(field).ShouldNotBeNull().SetValue(settings, color);

        // Act
        var result = BrandingSiteTheme.Create(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain(field);
    }

    [Fact]
    public void A_colour_that_is_not_a_hex_colour_is_refused_on_its_field()
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();
        settings.PrimaryColor = "url(javascript:alert(1))";

        // Act
        var result = BrandingSiteTheme.Create(settings);

        // Assert
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe([nameof(BrandingSettingsDto.PrimaryColor)]);
    }

    [Fact]
    public void A_font_outside_the_allowed_families_is_refused_on_its_field()
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();
        settings.HeadingFontFamily = "bad;font";

        // Act
        var result = BrandingSiteTheme.Create(settings);

        // Assert
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe([nameof(BrandingSettingsDto.HeadingFontFamily)]);
    }

    [Fact]
    public void A_draft_keeps_a_theme_that_breaks_a_contrast_rule_for_the_preview()
    {
        // Arrange
        var settings = BrandingSiteThemeTestData.CreateSettings();
        settings.TextColor = "#BBBBBB";

        // Act
        var draft = BrandingSiteTheme.CreateDraft(settings);

        // Assert
        draft.IsFailure.ShouldBeFalse();
        draft.Value.Review().Errors.ShouldNotBeEmpty();
    }
}
