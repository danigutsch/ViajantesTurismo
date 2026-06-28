using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiServiceTests.StepDefinitions;

[Binding]
public sealed class CatalogTourWorkflowSteps
{
    private readonly TestCatalogTourReadModelStore tourStore = new();
    private readonly TestPublicContentStore publicContentStore = new();
    private WebApplicationFactory<CatalogApiEntryPoint>? factory;
    private HttpClient? client;
    private Guid catalogTourId;
    private HttpResponseMessage? response;

    [Given("a catalog tour draft exists for identifier {string}")]
    public async Task Given_a_catalog_tour_draft_exists_for_identifier(string identifier)
    {
        catalogTourId = Guid.CreateVersion7();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                catalogTourId,
                Guid.CreateVersion7(),
                identifier,
                $"{identifier} draft",
                identifier,
                false,
                1,
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            TestContext.Current.CancellationToken);
    }

    [Given("no catalog tour draft exists")]
    public void Given_no_catalog_tour_draft_exists()
    {
        catalogTourId = Guid.CreateVersion7();
    }

    [When("I publish the catalog tour with title {string} and slug {string}")]
    public async Task When_I_publish_the_catalog_tour_with_title_and_slug(string title, string slug)
    {
        await PutPresentation(title, slug, true);
    }

    [When("I save the catalog tour presentation with title {string} and slug {string}")]
    public async Task When_I_save_the_catalog_tour_presentation_with_title_and_slug(string title, string slug)
    {
        await PutPresentation(title, slug, false);
    }

    [When("I try to publish a missing catalog tour")]
    public async Task When_I_try_to_publish_a_missing_catalog_tour()
    {
        await PutPresentation("Missing tour", "missing-tour", true);
    }

    [When("I try to publish the catalog tour without a title")]
    public async Task When_I_try_to_publish_the_catalog_tour_without_a_title()
    {
        await PutPresentation(string.Empty, "patagonia-2025", true);
    }

    [Then("the catalog tour should be available to catalog editors")]
    public async Task Then_the_catalog_tour_should_be_available_to_catalog_editors()
    {
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var managementResponse = await Client.GetAsync(new Uri($"/catalog/tours/{catalogTourId}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, managementResponse.StatusCode);

        var tour = await managementResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(tour);
        Assert.Equal(catalogTourId, tour.Id);
    }

    [Then("the catalog tour should be visible publicly by slug {string}")]
    public async Task Then_the_catalog_tour_should_be_visible_publicly_by_slug(string slug)
    {
        using var publicResponse = await Client.GetAsync(new Uri($"/public/catalog/tours/{slug}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

        var tour = await publicResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(tour);
        Assert.Equal(slug, tour.Slug);
        Assert.Equal(catalogTourId, tour.Id);
        Assert.True(tour.IsPublished);
    }

    [Then("the catalog tour should not be visible publicly by slug {string}")]
    public async Task Then_the_catalog_tour_should_not_be_visible_publicly_by_slug(string slug)
    {
        using var publicResponse = await Client.GetAsync(new Uri($"/public/catalog/tours/{slug}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
    }

    [Then("the catalog tour workflow should report that the tour is unavailable")]
    public void Then_the_catalog_tour_workflow_should_report_that_the_tour_is_unavailable()
    {
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Then("the catalog tour workflow should report a presentation validation problem")]
    public async Task Then_the_catalog_tour_workflow_should_report_a_presentation_validation_problem()
    {
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertCatalogTourPresentationRequest.Title), problem.Errors.Keys);
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

    private async Task PutPresentation(string title, string slug, bool isPublished)
    {
        response = await Client.PutAsJsonAsync(
            new Uri($"/catalog/tours/{catalogTourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = title,
                Slug = slug,
                IsPublished = isPublished
            },
            TestContext.Current.CancellationToken);
    }

    [AfterScenario]
    public void Dispose_client()
    {
        response?.Dispose();
        client?.Dispose();
        factory?.Dispose();
    }
}
