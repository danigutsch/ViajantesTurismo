using Microsoft.AspNetCore.Http;
using ContractCommandOutcomeKind = SharedKernel.HttpClients.ContractCommandOutcomeKind;

namespace ViajantesTurismo.Management.WebTests;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ApiClientCategory)]
public sealed class ToursApiClientTests
{
    [Fact]
    public async Task GetTours_requests_admin_tours_endpoint_and_skips_null_items()
    {
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.TourJson}, null]");
        });
        var sut = new ToursApiClient(httpClient);

        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        requestPath.ShouldBe("/api/v1/tours");
        var tour = tours.ShouldHaveSingleItem();
        tour.Identifier.ShouldBe("TOUR-1");
    }

    [Fact]
    public async Task GetTours_stops_when_max_items_is_reached()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.TourJson}, {AdminApiClientTestsHelpers.TourJson}]"));
        var sut = new ToursApiClient(httpClient);

        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken, maxItems: 1);

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

        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken, maxItems: 0);

        tours.ShouldBeEmpty();
        requestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetTourById_returns_tour_when_admin_api_returns_success()
    {
        var requestPath = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse(AdminApiClientTestsHelpers.TourJson);
        });
        var sut = new ToursApiClient(httpClient);

        var tour = await sut.GetTourById(tourId, Xunit.TestContext.Current.CancellationToken);

        tour.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/tours/11111111-1111-1111-1111-111111111111");
        tour.Name.ShouldBe("First tour");
    }

    [Fact]
    public async Task GetTourById_returns_null_when_admin_api_returns_not_found()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new ToursApiClient(httpClient);

        var tour = await sut.GetTourById(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        tour.ShouldBeNull();
    }

    [Fact]
    public async Task GetTourById_throws_when_admin_api_returns_success_with_null_body()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new ToursApiClient(httpClient);

        Func<Task> act = async () => await sut.GetTourById(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        var exception = await act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The tour response body was empty.");
    }

    [Fact]
    public async Task CreateTour_posts_tour_and_returns_success_outcome()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/api/v1/tours/11111111-1111-1111-1111-111111111111", UriKind.Relative) }
            };
        });
        var sut = new ToursApiClient(httpClient);

        // Act
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/tours");
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.Succeeded);
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
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.ValidationProblem);
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
        var outcome = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.Conflict);
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
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new ToursApiClient(httpClient);

        await sut.UpdateTour(tourId, AdminApiClientTestsHelpers.UpdateTour(), Xunit.TestContext.Current.CancellationToken);

        requestMethod.ShouldBe(HttpMethods.Put);
        requestPath.ShouldBe("/api/v1/tours/11111111-1111-1111-1111-111111111111");
    }
}
