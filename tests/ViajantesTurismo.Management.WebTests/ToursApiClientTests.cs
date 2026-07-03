using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Management.WebTests;

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

        requestPath.ShouldBe("/tours");
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
        requestPath.ShouldBe("/tours/11111111-1111-1111-1111-111111111111");
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
    public async Task CreateTour_posts_tour_and_returns_location()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/tours/11111111-1111-1111-1111-111111111111", UriKind.Relative) }
            };
        });
        var sut = new ToursApiClient(httpClient);

        var location = await sut.CreateTour(AdminApiClientTestsHelpers.CreateTour(), Xunit.TestContext.Current.CancellationToken);

        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/tours");
        location.ToString().ShouldBe("/tours/11111111-1111-1111-1111-111111111111");
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
        requestPath.ShouldBe("/tours/11111111-1111-1111-1111-111111111111");
    }
}
