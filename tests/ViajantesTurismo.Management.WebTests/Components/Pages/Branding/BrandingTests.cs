using BrandingPage = ViajantesTurismo.Management.Web.Components.Pages.Branding.Branding;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

public sealed class BrandingTests : BunitContext
{
    private readonly FakeBrandingApiClient brandingApi = new();

    public BrandingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IBrandingApiClient>(brandingApi);
    }

    [Fact]
    public void Renders_loaded_branding_values()
    {
        // Arrange
        brandingApi.Branding = new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = "https://cdn.example/logo.svg",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#112233", TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("#branding-brand-name").GetAttribute("value").ShouldBe("Camino Riders");
        cut.Find("#branding-logo-uri").GetAttribute("value").ShouldBe("https://cdn.example/logo.svg");
        cut.Find("#branding-primary-color").GetAttribute("value").ShouldBe("#112233");
        cut.Find("#branding-heading-font").GetAttribute("value").ShouldBe("Inter");
    }

    [Fact]
    public void Saves_branding_values()
    {
        // Arrange
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#branding-brand-name").Change("Camino Riders");
        cut.Find("#branding-logo-uri").Change("https://cdn.example/logo.svg");
        cut.Find("#branding-primary-color").Change("#112233");
        cut.Find("#branding-accent-color").Change("#445566");
        cut.Find("#branding-background-color").Change("#FFFFFF");
        cut.Find("#branding-text-color").Change("#000000");
        cut.Find("#branding-heading-font").Change("Inter");
        cut.Find("#branding-body-font").Change("Verdana");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => brandingApi.SavedBranding is not null, TimeSpan.FromSeconds(2));
        var savedBranding = brandingApi.SavedBranding.ShouldNotBeNull();
        savedBranding.BrandName.ShouldBe("Camino Riders");
        savedBranding.LogoUri.ShouldBe("https://cdn.example/logo.svg");
        savedBranding.PrimaryColor.ShouldBe("#112233");
        savedBranding.HeadingFontFamily.ShouldBe("Inter");
        cut.Markup.ShouldContain("Branding saved", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_accessible_labels_for_branding_inputs()
    {
        // Act
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Assert
        string[] inputIds =
        [
            "branding-brand-name",
            "branding-logo-uri",
            "branding-primary-color",
            "branding-accent-color",
            "branding-background-color",
            "branding-text-color",
            "branding-heading-font",
            "branding-body-font",
        ];

        foreach (var inputId in inputIds)
        {
            cut.Find($"label[for='{inputId}']").ShouldNotBeNull();
        }
    }

    [Fact]
    public void Shows_error_when_loaded_branding_response_is_empty()
    {
        // Arrange
        brandingApi.ReturnEmptyGetResponse = true;

        // Act
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Markup.Contains("couldn't load branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("couldn't load branding", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_error_when_saved_branding_response_is_empty()
    {
        // Arrange
        brandingApi.ReturnEmptySaveResponse = true;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("couldn't save branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("couldn't save branding", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_fallback_error_when_branding_validation_has_no_messages()
    {
        // Arrange
        brandingApi.ValidationException = new ContractValidationException("Validation problem response body was malformed.");
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("couldn't save branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save branding", StringComparison.Ordinal);
    }
}
