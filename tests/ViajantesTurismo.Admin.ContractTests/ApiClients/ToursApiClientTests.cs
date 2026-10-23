using Microsoft.AspNetCore.Http;
using HttpOutcomeKind = PandaBear.HttpClients.HttpOutcomeKind;
using CatalogToursApiClientTestsHelpers = PandaBear.Testing.Contracts.ContractHttpClientTestHelper;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class ToursApiClientTests
{
    [Fact]
    public async Task GetTours_requests_admin_tours_endpoint_and_skips_null_items()
    {
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.TourJson}, null]");
        });
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTours(TestContext.Current.CancellationToken);

        requestPath.ShouldBe("/api/v1/tours");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var tours = outcome.Value.ShouldNotBeNull();
        var tour = tours.ShouldHaveSingleItem();
        tour.Identifier.ShouldBe("TOUR-1");
    }

    [Fact]
    public async Task GetTours_stops_when_max_items_is_reached()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.TourJson}, {AdminApiClientTestsHelpers.TourJson}]"));
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTours(TestContext.Current.CancellationToken, maxItems: 1);

        var tours = outcome.Value.ShouldNotBeNull();
        tours.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetTours_returns_empty_without_request_when_max_items_is_zero()
    {
        var requestCount = 0;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
        {
            requestCount++;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.TourJson}]");
        });
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTours(TestContext.Current.CancellationToken, maxItems: 0);

        var tours = outcome.Value.ShouldNotBeNull();
        tours.ShouldBeEmpty();
        requestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetTourById_returns_tour_when_admin_API_returns_success()
    {
        var requestPath = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse(AdminApiClientTestsHelpers.TourJson);
        });
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTourById(tourId, TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var tour = outcome.Value.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/tours/11111111-1111-1111-1111-111111111111");
        tour.Name.ShouldBe("First tour");
    }

    [Fact]
    public async Task GetTourById_returns_null_when_admin_API_returns_not_found()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTourById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetTourById_returns_malformed_outcome_when_admin_API_returns_success_with_null_body()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.GetTourById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task CreateTour_posts_tour_and_returns_success_outcome()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/api/v1/tours/11111111-1111-1111-1111-111111111111", UriKind.Relative) }
            };
        });
        var sut = new ToursApiClient(httpClient);

        // Act
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/tours");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.Location.ShouldNotBeNull();
        outcome.Location.ToString().ShouldBe("/api/v1/tours/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task CreateTour_returns_validation_problem_outcome()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse(
                """
                {"errors":{"Name":["The Name field is required."]}}
                """,
                System.Net.HttpStatusCode.BadRequest));
        var sut = new ToursApiClient(httpClient);

        // Act
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.ValidationProblem);
        outcome.ValidationErrors.ShouldNotBeNull();
        outcome.ValidationErrors["Name"][0].ShouldBe("The Name field is required.");
    }

    [Fact]
    public async Task CreateTour_returns_status_outcome_for_conflict()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Conflict));
        var sut = new ToursApiClient(httpClient);

        // Act
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Conflict);
        outcome.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateTour_puts_tour_update()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new ToursApiClient(httpClient);

        var outcome = await sut.UpdateTour(tourId, AdminApiClientTestsHelpers.UpdateTour(), TestContext.Current.CancellationToken);

        requestMethod.ShouldBe(HttpMethods.Put);
        requestPath.ShouldBe("/api/v1/tours/11111111-1111-1111-1111-111111111111");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
    }
}
