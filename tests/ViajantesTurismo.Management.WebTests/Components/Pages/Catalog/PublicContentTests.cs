using PublicContent = ViajantesTurismo.Management.Web.Components.Pages.Catalog.PublicContent;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Exceptions;

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
        Assert.Contains("ReviewRequired", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Review-required text", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("We couldn't load public content", alert.TextContent, StringComparison.Ordinal);
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
        Assert.Equal("Welcome", cut.Find("#pt-br-title").GetAttribute("value"));
        Assert.Contains("Draft created and marked for human review", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Review-required text", cut.Markup, StringComparison.Ordinal);
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
        Assert.Equal("Bem-vindo", cut.Find("#en-us-title").GetAttribute("value"));
        Assert.Contains("Draft created and marked for human review", cut.Markup, StringComparison.Ordinal);
        Assert.True(cut.Find("#en-us-review").HasAttribute("checked"));
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
        Assert.Contains("Clear one target language title and body", cut.Find(".alert-danger").TextContent, StringComparison.Ordinal);
        Assert.Equal("Existing title", cut.Find("#pt-br-title").GetAttribute("value"));
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
            Assert.NotNull(cut.Find($"label[for='{inputId}']"));
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
        Assert.Equal("home.hero", publicContentApi.SavedKey);
        Assert.NotNull(publicContentApi.SavedRequest);
        Assert.Contains(publicContentApi.SavedRequest.Variants, variant => variant.Language == PublicContentLanguageDto.EnUs && variant.Title == "Welcome");
        Assert.Contains(publicContentApi.SavedRequest.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr && variant.Body == "Pedale conosco");
        Assert.Contains("Public content saved", cut.Markup, StringComparison.Ordinal);
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
        Assert.NotNull(publicContentApi.SavedRequest);
        var portugueseVariant = Assert.Single(publicContentApi.SavedRequest.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr);
        Assert.True(portugueseVariant.RequiresHumanReview);
        Assert.Equal("Welcome", portugueseVariant.Title);
        Assert.Contains("Public content saved", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("Confirm human review", cut.Find(".alert-danger").TextContent, StringComparison.Ordinal);
        Assert.Null(publicContentApi.SavedRequest);
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
        Assert.DoesNotContain("Confirm human review", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(publicContentApi.SavedRequest);
        var portugueseVariant = Assert.Single(publicContentApi.SavedRequest.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr);
        Assert.False(portugueseVariant.RequiresHumanReview);
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
        Assert.NotNull(publicContentApi.SavedRequest);
        var portugueseVariant = Assert.Single(publicContentApi.SavedRequest.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr);
        Assert.False(portugueseVariant.RequiresHumanReview);
    }

    [Fact]
    public void Shows_server_validation_message_when_save_fails_validation()
    {
        // Arrange
        publicContentApi.ValidationException = new ApiValidationException(
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
        Assert.Contains("Title is required", cut.Find(".alert-danger").TextContent, StringComparison.Ordinal);
    }

}
