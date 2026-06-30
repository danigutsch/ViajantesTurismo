using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicThemeSettingsTests
{
    [Fact]
    public void Create_accepts_safe_colors_and_allowed_fonts()
    {
        // Act
        var result = PublicThemeSettings.Create("#112233", "#AABBCC", "#FFFFFF", "#000000", "Inter", "Verdana");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("#112233", result.Value.PrimaryColor);
        Assert.Equal("Inter", result.Value.HeadingFontFamily);
    }

    [Fact]
    public void Create_normalizes_colors_and_allowed_fonts()
    {
        // Act
        var result = PublicThemeSettings.Create("  #1122aa  ", "#aabbcc", "#ffffff", "#000000", " inter ", "SYSTEM-UI");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("#1122AA", result.Value.PrimaryColor);
        Assert.Equal("#AABBCC", result.Value.AccentColor);
        Assert.Equal("Inter", result.Value.HeadingFontFamily);
        Assert.Equal("system-ui", result.Value.BodyFontFamily);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("url(javascript:alert(1))")]
    [InlineData("#12345G")]
    public void Create_rejects_unsafe_color_values(string color)
    {
        // Act
        var result = PublicThemeSettings.Create(color, "#AABBCC", "#FFFFFF", "#000000", "Inter", "Verdana");

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains(nameof(PublicThemeSettings.PrimaryColor), result.ErrorDetails.ValidationErrors?.Keys ?? []);
    }

    [Theory]
    [InlineData("Comic Sans MS")]
    [InlineData("Arial; background:url(javascript:alert(1))")]
    public void Create_rejects_unapproved_font_values(string fontFamily)
    {
        // Act
        var result = PublicThemeSettings.Create("#112233", "#AABBCC", "#FFFFFF", "#000000", fontFamily, "Verdana");

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains(nameof(PublicThemeSettings.HeadingFontFamily), result.ErrorDetails.ValidationErrors?.Keys ?? []);
    }
}
