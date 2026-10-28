using PublicContent = ViajantesTurismo.Management.Web.Components.Pages.Catalog.PublicContent;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

public sealed class PublicContentTests : BunitContext
{
    private readonly FakePublicContentApiClient publicContentApi = new();

    public PublicContentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPublicContentApiClient>(publicContentApi);
    }

    [Fact]
    public void Renders_loaded_public_content_entries_and_loads_selected_entry()
    {
        // Arrange
        publicContentApi.Content = [PublicContentTestsHelpers.CreateContent("home.hero")];

        // Act
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("home.hero", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("button.list-group-item").Click();

        // Assert
        cut.WaitForState(() => cut.Find("#content-key").GetAttribute("value") == "home.hero", TimeSpan.FromSeconds(2));
        cut.Markup.ShouldContain("ReviewRequired", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Review-required text", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_load_error_when_public_content_API_fails()
    {
        // Arrange
        publicContentApi.ThrowOnGetContent = true;

        // Act
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("couldn't load public content", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var alert = cut.Find(".alert-danger");
        alert.TextContent.ShouldContain("We couldn't load public content", StringComparison.Ordinal);
    }

    [Fact]
    public void Creates_review_draft_from_source_language_when_target_is_missing()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        cut.Find("#pt-br-title").GetAttribute("value").ShouldBe("Welcome");
        cut.Markup.ShouldContain("Starter draft copied from source content", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Review-required text", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("AI-assisted", StringComparison.Ordinal);
    }

    [Fact]
    public void Creates_review_draft_from_portuguese_source_when_english_target_is_missing()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#source-language").Change(PublicContentLanguageDto.PtBr.ToString());
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        cut.Find("#en-us-title").GetAttribute("value").ShouldBe("Bem-vindo");
        cut.Markup.ShouldContain("Starter draft copied from source content", StringComparison.Ordinal);
        cut.Find("#en-us-review").HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Asks_for_a_supported_source_language_when_the_loaded_entry_has_none()
    {
        // Arrange
        var content = PublicContentTestsHelpers.CreateContent("home.hero");
        content.SourceLanguage = PublicContentLanguageDto.None;
        publicContentApi.Content = [content];
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("home.hero", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("button.list-group-item").Click();
        cut.WaitForState(() => cut.Find("#content-key").GetAttribute("value") == "home.hero", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        cut.Markup.ShouldContain("Select a supported source language before generating a draft.", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Starter draft copied from source content", StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_overwrite_target_language_when_target_already_has_content()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Existing title");
        cut.Find("#pt-br-body").Change("Existing body");
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("Clear one target language title and body", StringComparison.Ordinal);
        cut.Find("#pt-br-title").GetAttribute("value").ShouldBe("Existing title");
    }

    [Fact]
    public void Renders_accessible_labels_for_public_content_inputs()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        string[] inputIds =
        [
            "content-key",
            "source-language",
            "en-us-title",
            "en-us-body",
            "en-us-seo-title",
            "en-us-meta-description",
            "en-us-share-summary",
            "en-us-review",
            "pt-br-title",
            "pt-br-body",
            "pt-br-seo-title",
            "pt-br-meta-description",
            "pt-br-share-summary",
            "pt-br-review",
        ];

        foreach (var inputId in inputIds)
        {
            cut.Find($"label[for='{inputId}']").ShouldNotBeNull();
        }
    }

    [Fact]
    public void Saves_public_content_with_both_language_variants()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => publicContentApi.SavedRequest is not null, TimeSpan.FromSeconds(2));
        publicContentApi.SavedKey.ShouldBe("home.hero");
        publicContentApi.SavedRequest.ShouldNotBeNull();
        publicContentApi.SavedRequest.Variants.ShouldContain(variant => variant.Language == PublicContentLanguageDto.EnUs && variant.Title == "Welcome");
        publicContentApi.SavedRequest.Variants.ShouldContain(variant => variant.Language == PublicContentLanguageDto.PtBr && variant.Body == "Pedale conosco");
        cut.Markup.ShouldContain("Public content saved", StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_one_language_creates_review_required_missing_language_draft()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => publicContentApi.SavedRequest is not null, TimeSpan.FromSeconds(2));
        publicContentApi.SavedRequest.ShouldNotBeNull();
        var portugueseVariant = publicContentApi.SavedRequest.Variants.ShouldHaveSingleItem(variant => variant.Language == PublicContentLanguageDto.PtBr);
        portugueseVariant.RequiresHumanReview.ShouldBeTrue();
        portugueseVariant.Title.ShouldBe("Welcome");
        cut.Markup.ShouldContain("Public content saved", StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_blank_languages_shows_draft_source_guidance()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("form").Submit();

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("Enter source title and body before generating a draft", StringComparison.Ordinal);
        publicContentApi.SavedRequest.ShouldBeNull();
    }

    [Fact]
    public void Clearing_generated_draft_review_flag_requires_human_review_confirmation()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("button.btn-outline-primary").Click();
        cut.Find("#pt-br-review").Change(false);
        cut.Find("#content-key").Change("home.hero");
        cut.Find("form").Submit();

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldContain("Confirm human review", StringComparison.Ordinal);
        publicContentApi.SavedRequest.ShouldBeNull();
    }

    [Fact]
    public void Persisted_review_required_content_can_be_saved_without_generated_draft_confirmation()
    {
        // Arrange
        publicContentApi.Content = [PublicContentTestsHelpers.CreateContent("home.hero")];
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("home.hero", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("button.list-group-item").Click();
        cut.WaitForState(() => cut.Find("#content-key").GetAttribute("value") == "home.hero", TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#pt-br-review").Change(false);
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => publicContentApi.SavedRequest is not null, TimeSpan.FromSeconds(2));
        cut.Markup.ShouldNotContain("Confirm human review", StringComparison.Ordinal);
        publicContentApi.SavedRequest.ShouldNotBeNull();
        var portugueseVariant = publicContentApi.SavedRequest.Variants.ShouldHaveSingleItem(variant => variant.Language == PublicContentLanguageDto.PtBr);
        portugueseVariant.RequiresHumanReview.ShouldBeFalse();
    }

    [Fact]
    public void Confirmed_generated_draft_can_be_saved_as_human_reviewed()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("button.btn-outline-primary").Click();
        cut.Find("#pt-br-review").Change(false);
        cut.Find("#pt-br-review-confirmed").Change(true);
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => publicContentApi.SavedRequest is not null, TimeSpan.FromSeconds(2));
        publicContentApi.SavedRequest.ShouldNotBeNull();
        var portugueseVariant = publicContentApi.SavedRequest.Variants.ShouldHaveSingleItem(variant => variant.Language == PublicContentLanguageDto.PtBr);
        portugueseVariant.RequiresHumanReview.ShouldBeFalse();
    }

    [Fact]
    public void Shows_fixed_copy_instead_of_server_validation_text_when_save_fails_validation()
    {
        // Arrange
        var hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42 " + new string('x', 5000);
        publicContentApi.ValidationException = new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { [nameof(PublicContentVariantDto.Title)] = [hostile] });
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-danger").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save public content", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("alert(1)", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Secret.cs", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_the_server_field_message_next_to_the_content_key_and_links_it_from_a_focused_summary()
    {
        // Arrange
        publicContentApi.ValidationException = new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { ["key"] = ["Key is already used."] });
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll("#error-summary").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        var fieldGroup = cut.Find("#content-key").ParentElement.ShouldNotBeNull();
        fieldGroup.QuerySelector(".validation-message").ShouldNotBeNull().TextContent.ShouldBe("Key is already used.");
        cut.FindAll(".alert-danger").ShouldBeEmpty();
        var summary = cut.Find("#error-summary");
        summary.GetAttribute("tabindex").ShouldBe("-1");
        cut.Find("#error-summary a").GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#content-key", StringComparison.Ordinal);
        JSInterop.Invocations.ShouldContain(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void Clears_the_error_summary_when_a_new_entry_replaces_the_form_model()
    {
        // Arrange
        publicContentApi.ValidationException = new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { ["key"] = ["Key is already used."] });
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll("#error-summary").Count == 1, TimeSpan.FromSeconds(2));

        // Act
        var newEntryButtons = cut.FindAll("button").Where(button => button.TextContent.Trim() == "New entry").ToList();
        newEntryButtons.ShouldHaveSingleItem().Click();

        // Assert
        cut.FindAll("#error-summary").ShouldBeEmpty();
    }

    [Fact]
    public void Truncates_hostile_server_field_text_and_never_renders_it_as_markup()
    {
        // Arrange
        var hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42 " + new string('x', 5000);
        publicContentApi.ValidationException = new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { ["Key"] = [hostile] });
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();
        cut.WaitForState(() => cut.FindAll(".validation-message").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        cut.FindAll("script").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("<script", StringComparison.Ordinal);
        var fieldMessage = cut.Find("#content-key").ParentElement.ShouldNotBeNull().QuerySelector(".validation-message").ShouldNotBeNull();
        fieldMessage.TextContent.Length.ShouldBeLessThanOrEqualTo(300);
    }

    [Fact]
    public void Shows_fallback_error_when_content_validation_has_no_messages()
    {
        // Arrange
        publicContentApi.ValidationException = new ContractValidationException("Validation problem response body was malformed.");
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("couldn't save public content", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find(".alert-danger").TextContent.ShouldContain("couldn't save public content", StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_submit_shows_a_focused_error_summary_linking_to_the_content_key_without_saving()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find("#error-summary").GetAttribute("tabindex").ShouldBe("-1");
        var link = cut.Find("#error-summary a");
        link.GetAttribute("href").ShouldNotBeNull().ShouldEndWith("#content-key", StringComparison.Ordinal);
        publicContentApi.SavedRequest.ShouldBeNull();
        JSInterop.Invocations.ShouldContain(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
    }

    [Fact]
    public void Shows_not_found_copy_when_the_selected_entry_no_longer_exists()
    {
        // Arrange
        publicContentApi.Content = [PublicContentTestsHelpers.CreateContent("home.hero")];
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("home.hero", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        publicContentApi.Content = [];

        // Act
        cut.Find("button.list-group-item").Click();

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldBe("Public content entry was not found.");
        string.IsNullOrEmpty(cut.Find("#content-key").GetAttribute("value")).ShouldBeTrue();
    }

    [Fact]
    public void Rejects_an_edited_language_with_a_title_but_no_body_without_saving()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.Find(".alert-danger").TextContent.ShouldBe("Enter both title and body for each edited language.");
        publicContentApi.SavedRequest.ShouldBeNull();
    }

    [Fact]
    public void Shows_fixed_copy_and_no_success_when_saving_fails_in_transport()
    {
        // Arrange
        publicContentApi.ThrowOnSaveContent = true;
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("#content-key").Change("home.hero");
        cut.Find("#en-us-title").Change("Welcome");
        cut.Find("#en-us-body").Change("Ride with us");
        cut.Find("#pt-br-title").Change("Bem-vindo");
        cut.Find("#pt-br-body").Change("Pedale conosco");

        // Act
        cut.Find("form").Submit();

        // Assert
        cut.WaitForState(() => cut.FindAll(".alert-danger").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find(".alert-danger").TextContent.ShouldBe("We couldn't save public content right now. Please try again.");
        cut.Markup.ShouldNotContain("HTTP transport failed", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Public content saved", StringComparison.Ordinal);
        publicContentApi.SavedRequest.ShouldBeNull();
    }
}
