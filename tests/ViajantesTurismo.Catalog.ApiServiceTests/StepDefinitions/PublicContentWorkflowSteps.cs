using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiServiceTests.StepDefinitions;

[Binding]
public sealed class PublicContentWorkflowSteps
{
    private readonly TestCatalogTourReadModelStore tourStore = new();
    private readonly TestPublicContentStore publicContentStore = new();
    private WebApplicationFactory<CatalogApiEntryPoint>? factory;
    private HttpClient? client;
    private string contentKey = string.Empty;
    private UpsertPublicContentRequest? request;
    private HttpResponseMessage? response;
    private PublicContentDto? savedContent;

    [Given("localized public content for key {string} includes English and Portuguese variants")]
    public void Given_localized_public_content_for_key_includes_english_and_portuguese_variants(string key)
    {
        contentKey = key;
        request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true });
    }

    [Given("localized public content for key {string} includes only English")]
    public void Given_localized_public_content_for_key_includes_only_english(string key)
    {
        contentKey = key;
        request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
    }

    [When("I save the public content")]
    public async Task When_I_save_the_public_content()
    {
        Assert.NotNull(request);
        response = await Client.PutAsJsonAsync(
            new Uri($"/catalog/public-content/{contentKey}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
    }

    [Then("the public content should be stored for key {string}")]
    public async Task Then_the_public_content_should_be_stored_for_key(string expectedKey)
    {
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(content);
        Assert.Equal(expectedKey, content.Key);
        savedContent = content;
    }

    [Then("the public content should require review before publication")]
    public void Then_the_public_content_should_require_review_before_publication()
    {
        Assert.NotNull(savedContent);
        Assert.Equal("ReviewRequired", savedContent.PublicationState);
    }

    [Then("the public content workflow should report a localization validation problem")]
    public async Task Then_the_public_content_workflow_should_report_a_localization_validation_problem()
    {
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Variants", problem.Errors.Keys);
    }

    private HttpClient Client
    {
        get
        {
            if (client is not null)
            {
                return client;
            }

            factory = CatalogApiTestHost.Create(tourStore, publicContentStore);
            client = factory.CreateClient();

            return client;
        }
    }

    [AfterScenario]
    public void Dispose_client()
    {
        response?.Dispose();
        client?.Dispose();
        factory?.Dispose();
    }
}
