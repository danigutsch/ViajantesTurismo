using System.Net;
using System.Text;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

public sealed class CatalogToursApiClientTests
{
    [Fact]
    public async Task GetTours_Requests_Management_Catalog_Endpoint_And_Skips_Null_Items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CreateClient((request, _) =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return JsonResponse("""
                [
                  {
                    "id":"11111111-1111-1111-1111-111111111111",
                    "adminTourId":"22222222-2222-2222-2222-222222222222",
                    "identifier":"TOUR-1",
                    "title":"First tour",
                    "slug":"first-tour",
                    "isPublished":false,
                    "images":[],
                    "updatedAt":"2026-06-25T10:00:00+00:00"
                  },
                  null,
                  {
                    "id":"33333333-3333-3333-3333-333333333333",
                    "adminTourId":"44444444-4444-4444-4444-444444444444",
                    "identifier":"TOUR-2",
                    "title":"Second tour",
                    "slug":"second-tour",
                    "isPublished":true,
                    "images":[],
                    "updatedAt":"2026-06-25T11:00:00+00:00"
                  }
                ]
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/catalog/tours", requestPath);
        Assert.Collection(
            tours,
            tour => Assert.Equal("first-tour", tour.Slug),
            tour => Assert.Equal("second-tour", tour.Slug));
    }

    [Fact]
    public async Task GetTours_Returns_Empty_Array_When_Catalog_Returns_Only_Nulls()
    {
        // Arrange
        using var httpClient = CreateClient((_, _) => JsonResponse("[null,null]"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(tours);
    }

    private static StubHttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
    {
        return new StubHttpClient(handler)
        {
            BaseAddress = new Uri("https://catalog.example")
        };
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request, cancellationToken));
        }
    }

    private sealed class StubHttpClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpClient(
        new StubHttpMessageHandler(handler), disposeHandler: true);
}
