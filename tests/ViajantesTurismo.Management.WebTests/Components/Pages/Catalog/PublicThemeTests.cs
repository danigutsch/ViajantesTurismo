using PublicTheme = ViajantesTurismo.Management.Web.Components.Pages.Catalog.PublicTheme;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

public sealed class PublicThemeTests : BunitContext
{
    private readonly FakePublicThemeApiClient publicThemeApi = new();

    public PublicThemeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPublicThemeApiClient>(publicThemeApi);
    }

    [Fact]
    public void Renders_loaded_public_theme_values()
    {
        // Arrange
        publicThemeApi.Theme = new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        var cut = Render<PublicTheme>();
        cut.WaitForState(() => cut.Find("#theme-primary-color").GetAttribute("value") == "#112233", TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal("#112233", cut.Find("#theme-primary-color").GetAttribute("value"));
        Assert.Equal("Inter", cut.Find("#theme-heading-font").GetAttribute("value"));
    }

    [Fact]
    public void Saves_public_theme_values()
    {
        // Arrange
        var cut = Render<PublicTheme>();
        cut.WaitForState(() => cut.Find("#theme-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#theme-primary-color").Change("#112233");
        cut.Find("#theme-accent-color").Change("#445566");
        cut.Find("#theme-background-color").Change("#FFFFFF");
        cut.Find("#theme-text-color").Change("#000000");
        cut.Find("#theme-heading-font").Change("Inter");
        cut.Find("#theme-body-font").Change("Verdana");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => publicThemeApi.SavedTheme is not null, TimeSpan.FromSeconds(2));
        Assert.NotNull(publicThemeApi.SavedTheme);
        Assert.Equal("#112233", publicThemeApi.SavedTheme.PrimaryColor);
        Assert.Equal("Inter", publicThemeApi.SavedTheme.HeadingFontFamily);
        Assert.Contains("Public theme saved", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_accessible_labels_for_public_theme_inputs()
    {
        // Act
        var cut = Render<PublicTheme>();
        cut.WaitForState(() => cut.Find("#theme-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Assert
        string[] inputIds =
        [
            "theme-primary-color",
            "theme-accent-color",
            "theme-background-color",
            "theme-text-color",
            "theme-heading-font",
            "theme-body-font",
        ];

        foreach (var inputId in inputIds)
        {
            Assert.NotNull(cut.Find($"label[for='{inputId}']"));
        }
    }

    [Fact]
    public void Shows_error_when_loaded_public_theme_response_is_empty()
    {
        // Arrange
        publicThemeApi.ReturnEmptyGetResponse = true;

        // Act
        var cut = Render<PublicTheme>();
        cut.WaitForState(() => cut.Markup.Contains("couldn't load public theme", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("couldn't load public theme", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_error_when_saved_public_theme_response_is_empty()
    {
        // Arrange
        publicThemeApi.ReturnEmptySaveResponse = true;
        var cut = Render<PublicTheme>();
        cut.WaitForState(() => cut.Find("#theme-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("couldn't save public theme", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("couldn't save public theme", cut.Markup, StringComparison.Ordinal);
    }
}
