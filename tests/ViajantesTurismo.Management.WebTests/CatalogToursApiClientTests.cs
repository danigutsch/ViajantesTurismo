using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

public sealed class CatalogToursApiClientTests
{
    [Fact]
    public async Task GetTours_Requests_Management_Catalog_Endpoint_And_Skips_Null_Items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
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
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("[null,null]"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(tours);
    }

    internal sealed class StubHttpClient : HttpClient
    {
        private readonly IHost host;

        public StubHttpClient(IHost host) : base(host.GetTestServer().CreateHandler(), disposeHandler: true)
        {
            this.host = host;
            BaseAddress = new Uri("https://catalog.example");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                host.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    internal static class CatalogToursApiClientTestsHelpers
    {
        public static StubHttpClient CreateClient(Func<HttpRequest, HttpResponseMessage> handler)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder => builder
                    .UseTestServer()
                    .Configure(app => app.Run(async context =>
                    {
                        using var response = handler(context.Request);

                        context.Response.StatusCode = (int)response.StatusCode;

                        if (response.Content is not null)
                        {
                            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
                            var content = await response.Content.ReadAsStringAsync(context.RequestAborted);
                            await context.Response.WriteAsync(content, context.RequestAborted);
                        }
                    })))
                .Start();

            return new StubHttpClient(host);
        }

        public static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
