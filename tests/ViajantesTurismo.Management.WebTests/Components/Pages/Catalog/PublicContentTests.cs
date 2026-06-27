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
    public void Renders_Loaded_Public_Content_Entries_And_Loads_Selected_Entry()
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
    public void Renders_Load_Error_When_Public_Content_Api_Fails()
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
    public void Creates_Review_Draft_From_Source_Language_When_Target_Is_Missing()
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
    public void Creates_Review_Draft_From_Portuguese_Source_When_English_Target_Is_Missing()
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
    public void Does_Not_Overwrite_Target_Language_When_Target_Already_Has_Content()
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
    public void Renders_Accessible_Labels_For_Public_Content_Inputs()
    {
        // Arrange
        var cut = Render<PublicContent>();
        cut.WaitForState(() => cut.Markup.Contains("No public content entries yet", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        void AssertLabelFor(string id)
        {
            Assert.NotNull(cut.Find($"label[for='{id}']"));
        }

        // Assert
        AssertLabelFor("content-key");
        AssertLabelFor("source-language");
        AssertLabelFor("en-us-title");
        AssertLabelFor("en-us-body");
        AssertLabelFor("en-us-seo-title");
        AssertLabelFor("en-us-meta-description");
        AssertLabelFor("en-us-share-summary");
        AssertLabelFor("en-us-review");
        AssertLabelFor("pt-br-title");
        AssertLabelFor("pt-br-body");
        AssertLabelFor("pt-br-seo-title");
        AssertLabelFor("pt-br-meta-description");
        AssertLabelFor("pt-br-share-summary");
        AssertLabelFor("pt-br-review");
    }

    [Fact]
    public void Saves_Public_Content_With_Both_Language_Variants()
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
    public void Shows_Server_Validation_Message_When_Save_Fails_Validation()
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
