using PublicContent = ViajantesTurismo.Management.Web.Components.Pages.Catalog.PublicContent;
using ViajantesTurismo.Common.Contracts;

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
    public void Renders_load_error_when_public_content_api_fails()
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
        cut.Markup.ShouldNotContain("AI-assisted");
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
        cut.Markup.ShouldNotContain("Confirm human review");
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
    public void Shows_server_validation_message_when_save_fails_validation()
    {
        // Arrange
        publicContentApi.ValidationException = new ContractValidationException(
            "Validation failed",
            new Dictionary<string, string[]> { [nameof(PublicContentVariantDto.Title)] = ["Title is required."] });
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
        cut.WaitForState(() => cut.Markup.Contains("Title is required", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find(".alert-danger").TextContent.ShouldContain("Title is required", StringComparison.Ordinal);
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

}
