using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.BehaviorTests.StepDefinitions;

[Binding]
public sealed class PublicContentWorkflowSteps
{
    private readonly TestCatalogTourReadModelStore tourStore = new();
    private readonly TestPublicContentStore publicContentStore = new();
    private WebApplicationFactory<CatalogApiHostEntryPoint>? factory;
    private HttpClient? client;
    private string contentKey = string.Empty;
    private UpsertPublicContentRequest? request;
    private HttpResponseMessage? response;
    private PublicContentDto? savedContent;

    [Given("localized public content for key {string} includes English and Portuguese variants")]
    public void GivenLocalizedPublicContentForKeyIncludesEnglishAndPortugueseVariants(string key)
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
    public void GivenLocalizedPublicContentForKeyIncludesOnlyEnglish(string key)
    {
        contentKey = key;
        request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
    }

    [When("I save the public content")]
    public async Task WhenISaveThePublicContent()
    {
        request.ShouldNotBeNull();
        response = await Client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/public-content/{contentKey}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
    }

    [Then("the public content should be stored for key {string}")]
    public async Task ThenThePublicContentShouldBeStoredForKey(string expectedKey)
    {
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        content.ShouldNotBeNull();
        content.Key.ShouldBe(expectedKey);
        savedContent = content;
    }

    [Then("the public content should require review before publication")]
    public void ThenThePublicContentShouldRequireReviewBeforePublication()
    {
        savedContent.ShouldNotBeNull();
        savedContent.PublicationState.ShouldBe("ReviewRequired");
    }

    [Then("the public content workflow should report a localization validation problem")]
    public async Task ThenThePublicContentWorkflowShouldReportALocalizationValidationProblem()
    {
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain("Variants");
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
    public void DisposeClient()
    {
        response?.Dispose();
        client?.Dispose();
        factory?.Dispose();
    }
}
