using Microsoft.AspNetCore.Mvc.Testing;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class PublicWebEndpointTests
{
    [Fact]
    public async Task Root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html lang=\"en\">", content, StringComparison.Ordinal);
        Assert.Contains("Viajantes Turismo", content, StringComparison.Ordinal);
        Assert.Contains("Cycle tourism around the world!", content, StringComparison.Ordinal);
        Assert.Contains("New tours will be published soon.", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h3><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h3>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tours could not be loaded right now. Try again later.", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/group-bike-tours", "Group Bike Tours")]
    [InlineData("/gallery", "Gallery")]
    public async Task Public_ssr_routes_return_expected_content(string path, string expectedHeading)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains($"<h1>{expectedHeading}</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h2>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tours could not be loaded right now. Try again later.", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_tour_content_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Camino Norte</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_unavailable_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailDetailsRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Tour unavailable</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_not_found_when_tour_is_not_published()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Tour not found</h1>", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_health_endpoint_returns_success(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_endpoint_returns_problem_response()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri("/Error", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Production_root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(environment: "Production");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_default_health_endpoint_is_not_exposed(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(environment: "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
