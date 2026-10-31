using System.Net;
using PandaBear.Testing;
using BrandingPage = ViajantesTurismo.Management.Web.Components.Pages.Branding.Branding;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Branding;

[TestScope(TestScope.Component)]
public sealed class BrandingTests : BunitContext
{
    private const string GeneratedLogoUri = "/api/v1/public/branding/logo/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

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
            LogoUri = GeneratedLogoUri,
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
        cut.Find("#branding-logo-status").TextContent.ShouldBe("A logo is set.");
        cut.FindAll("#branding-logo-uri").ShouldBeEmpty();
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
        savedBranding.LogoUri.ShouldBeNull();
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
            "branding-logo-file",
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
    public void Does_not_save_without_an_entity_tag_after_the_initial_load_failed()
    {
        // Arrange
        brandingApi.ReturnEmptyGetResponse = true;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Markup.Contains("couldn't load branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.Markup.Contains("couldn't save branding", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        brandingApi.SavedEntityTag.ShouldBeNull();
        brandingApi.SavedBranding.ShouldBeNull();
        cut.Find(".alert-danger").TextContent.ShouldBe("We couldn't save branding right now. Please try again.");
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
    public void Replaces_a_stack_trace_like_server_field_message_with_the_fixed_field_message()
    {
        // Arrange
        brandingApi.ValidationErrors = new Dictionary<string, string[]>
        {
            ["PrimaryColor"] = ["System.NullReferenceException: Object reference not set\n   at Server.Internal.Handler() in /src/Secret.cs:line 42"]
        };
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".validation-message").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var fieldMessage = cut.Find("#branding-primary-color").ParentElement.ShouldNotBeNull().QuerySelector(".validation-message").ShouldNotBeNull();
        fieldMessage.TextContent.ShouldBe("This value is not valid. Check it and try again.");
        cut.Markup.ShouldNotContain("Secret.cs", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("NullReferenceException", StringComparison.Ordinal);
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
        var heading = cut.Find("#error-summary h2");
        summary.GetAttribute("aria-labelledby").ShouldBe(heading.Id);
        heading.TextContent.ShouldBe("There is a problem");
        var link = cut.Find("#error-summary a");
        link.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#branding-primary-color", StringComparison.Ordinal);
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

    [Fact]
    public void Invalid_submit_shows_a_focused_error_summary_linking_to_the_field_without_saving()
    {
        // Arrange
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));
        cut.Find("#branding-brand-name").Change(string.Empty);

        // Act
        cut.Find("form").Submit();

        // Assert
        var summary = cut.Find("#error-summary");
        summary.GetAttribute("tabindex").ShouldBe("-1");
        var link = cut.Find("#error-summary a");
        link.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#branding-brand-name", StringComparison.Ordinal);
        brandingApi.SavedEntityTag.ShouldBeNull();
        brandingApi.SavedBranding.ShouldBeNull();
        JSInterop.Invocations.ShouldContain(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void Shows_fixed_save_error_and_no_success_when_the_read_after_a_successful_save_fails()
    {
        // Arrange
        brandingApi.ReturnEmptyGetResponseAfterSave = true;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));
        cut.Find("#branding-brand-name").Change("Camino Riders");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-danger").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find(".alert-danger").TextContent.ShouldBe("We couldn't save branding right now. Please try again.");
        cut.Markup.ShouldNotContain("Branding saved", StringComparison.Ordinal);
    }

    [Fact]
    public void Offers_an_accessible_image_upload_with_hints_and_a_live_status()
    {
        // Act
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Assert
        var input = cut.Find("#branding-logo-file");
        input.GetAttribute("accept").ShouldBe("image/png,image/jpeg,image/webp");
        input.GetAttribute("aria-describedby").ShouldBe("branding-logo-help branding-logo-status");
        cut.Find("#branding-logo-help").TextContent.ShouldContain("1 MiB", StringComparison.Ordinal);
        cut.Find("#branding-logo-status").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("#branding-logo-status").TextContent.ShouldBe("No logo is set.");
        cut.FindAll("button:contains('Remove logo')").ShouldBeEmpty();
    }

    [Fact]
    public void Uploads_the_chosen_image_with_the_loaded_entity_tag()
    {
        // Arrange
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // Act
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(png, "logo.png", contentType: "image/png"));

        // Assert
        cut.WaitForState(() => brandingApi.UploadedContent is not null, TimeSpan.FromSeconds(2));
        brandingApi.UploadedContentType.ShouldBe("image/png");
        brandingApi.UploadedContent.ShouldBe(png);
        brandingApi.UploadedEntityTag.ShouldBe(BrandingEntityTags.Format(brandingApi.SourceEpoch, 1));
        cut.WaitForState(() => cut.Markup.Contains("Logo uploaded.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#branding-logo-status").TextContent.ShouldBe("A logo is set.");
    }

    [Fact]
    public void Shows_fixed_error_copy_when_the_server_refuses_the_image()
    {
        // Arrange
        brandingApi.UploadFailureStatus = HttpStatusCode.BadRequest;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "logo.png", contentType: "image/png"));

        // Assert
        cut.WaitForState(() => cut.FindAll("#branding-logo-error").Count == 1, TimeSpan.FromSeconds(2));
        var error = cut.Find("#branding-logo-error");
        error.GetAttribute("role").ShouldBe("alert");
        error.TextContent.ShouldContain("PNG, JPEG or WebP", StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_a_stale_entity_tag_when_uploading()
    {
        // Arrange
        brandingApi.UploadFailureStatus = HttpStatusCode.PreconditionFailed;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.Find("#branding-primary-color").GetAttribute("value") == "#0F766E", TimeSpan.FromSeconds(2));

        // Act
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1], "logo.png", contentType: "image/png"));

        // Assert
        cut.WaitForState(() => cut.FindAll("#branding-logo-error").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("#branding-logo-error").TextContent.ShouldContain("changed by someone else", StringComparison.Ordinal);
    }

    [Fact]
    public void Removing_the_logo_saves_the_form_with_no_logo_uri()
    {
        // Arrange
        brandingApi.Branding.LogoUri = GeneratedLogoUri;
        var cut = Render<BrandingPage>();
        cut.WaitForState(() => cut.FindAll("button:contains('Remove logo')").Count == 1, TimeSpan.FromSeconds(2));

        // Act
        cut.Find("button:contains('Remove logo')").Click();

        // Assert
        cut.WaitForState(() => brandingApi.SavedBranding is not null, TimeSpan.FromSeconds(2));
        brandingApi.SavedBranding.ShouldNotBeNull().LogoUri.ShouldBeNull();
        cut.WaitForState(() => cut.Find("#branding-logo-status").TextContent == "No logo is set.", TimeSpan.FromSeconds(2));
    }
}
