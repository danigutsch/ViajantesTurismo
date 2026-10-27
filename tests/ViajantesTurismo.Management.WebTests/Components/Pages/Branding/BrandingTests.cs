using BrandingPage = ViajantesTurismo.Management.Web.Components.Pages.Branding.Branding;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

public sealed class BrandingTests : BunitContext
{
    private readonly FakeBrandingApiClient brandingApi = new();

    public BrandingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IBrandingManagementClient>(brandingApi);
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
    public void Save_sends_the_loaded_entity_tag_and_refreshes_the_validator_with_a_new_read()
    {
        // Arrange
        var loadedTag = BrandingEntityTags.Format(brandingApi.SourceEpoch, 1);
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#branding-brand-name").Change("First edit");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("Branding saved", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        var firstTag = brandingApi.SavedEntityTag;
        cut.Find("#branding-brand-name").Change("Second edit");
        cut.Find("form").Submit();
        cut.WaitForState(() => brandingApi.SourceRevision == 3, TimeSpan.FromSeconds(2));

        // Assert
        firstTag.ShouldBe(loadedTag);
        brandingApi.SavedEntityTag.ShouldBe(BrandingEntityTags.Format(brandingApi.SourceEpoch, 2));
        brandingApi.GetCount.ShouldBe(3);
    }

    [Fact]
    public void Shows_a_reload_message_and_keeps_the_edit_when_another_editor_saved_first()
    {
        // Arrange
        brandingApi.FailPrecondition = true;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#branding-brand-name").Change("Stale edit");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("changed by someone else", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#branding-brand-name").GetAttribute("value").ShouldBe("Stale edit");
        brandingApi.SavedBranding.ShouldBeNull();
        brandingApi.GetCount.ShouldBe(1);
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
    public void Shows_fixed_copy_instead_of_server_validation_text_when_branding_is_rejected()
    {
        // Arrange
        var hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42 " + new string('x', 5000);
        brandingApi.ValidationErrors = new Dictionary<string, string[]> { ["UnmappedField"] = [hostile] };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".alert-danger").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save branding", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("alert(1)", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Secret.cs", StringComparison.Ordinal);
        brandingApi.SavedBranding.ShouldBeNull();
    }

    [Fact]
    public void Shows_the_server_field_message_next_to_the_matching_field_and_no_free_form_alert()
    {
        // Arrange
        brandingApi.ValidationErrors = new Dictionary<string, string[]> { ["primaryColor"] = ["Use a safe color."] };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".validation-message").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var fieldGroup = cut.Find("#branding-primary-color").ParentElement.ShouldNotBeNull();
        fieldGroup.QuerySelector(".validation-message").ShouldNotBeNull().TextContent.ShouldBe("Use a safe color.");
        cut.FindAll(".alert-danger").ShouldBeEmpty();
    }

    [Fact]
    public void Truncates_hostile_server_field_text_and_never_renders_it_as_markup()
    {
        // Arrange
        var hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42 " + new string('x', 5000);
        brandingApi.ValidationErrors = new Dictionary<string, string[]> { ["PrimaryColor"] = [hostile] };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".validation-message").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        cut.FindAll("script").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("<script", StringComparison.Ordinal);
        var fieldMessage = cut.Find("#branding-primary-color").ParentElement.ShouldNotBeNull().QuerySelector(".validation-message").ShouldNotBeNull();
        fieldMessage.TextContent.Length.ShouldBeLessThanOrEqualTo(300);
    }

    [Fact]
    public void Falls_back_to_fixed_copy_when_the_server_field_key_is_unknown()
    {
        // Arrange
        brandingApi.ValidationErrors = new Dictionary<string, string[]> { ["notAField"] = ["Secret rule text."] };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".alert-danger").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save branding", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Secret rule text.", StringComparison.Ordinal);
    }

    [Fact]
    public void Error_summary_links_to_the_field_and_receives_focus()
    {
        // Arrange
        brandingApi.ValidationErrors = new Dictionary<string, string[]> { ["PrimaryColor"] = ["Use a safe color."] };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll("#error-summary").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        var summary = cut.Find("#error-summary");
        summary.GetAttribute("tabindex").ShouldBe("-1");
        summary.GetAttribute("aria-labelledby").ShouldBe("error-summary-title");
        var link = cut.Find("#error-summary a");
        link.GetAttribute("href").ShouldBe("#branding-primary-color");
        link.TextContent.ShouldBe("Use a safe color.");
        JSInterop.Invocations.ShouldContain(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void Shows_fallback_error_when_branding_validation_has_no_messages()
    {
        // Arrange
        brandingApi.ReturnMalformedValidationProblem = true;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("couldn't save branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save branding", StringComparison.Ordinal);
    }
}
