using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraits.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraits.HostName, TestTraits.TestServerHost)]
public sealed class PublicWebEndpointTests
{
    [Fact]
    public async Task Root_Returns_Public_Landing_Page()
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create();
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

    [Theory]
    [InlineData("/group-bike-tours", "Group Bike Tours")]
    [InlineData("/gallery", "Gallery")]
    public async Task Public_Ssr_Routes_Return_Expected_Content(string path, string expectedHeading)
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create();
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
    public async Task Public_Tour_Details_Returns_Tour_Content_When_Catalog_Loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.Create(catalogApiClient: catalogApi);
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
    public async Task Public_Tour_Details_Returns_Unavailable_When_Catalog_Fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailDetailsRequests = true };

        await using var factory = PublicWebTestHost.Create(catalogApiClient: catalogApi);
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

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_Health_Endpoint_Returns_Success(string path)
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_Endpoint_Returns_Problem_Response()
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create();
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
    public async Task Production_Root_Returns_Public_Landing_Page()
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create("Production");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_Default_Health_Endpoint_Is_Not_Exposed(string path)
    {
        // Arrange
        await using var factory = PublicWebTestHost.Create("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CatalogTourDto CreateTour(string slug, string title)
    {
        return new CatalogTourDto
        {
            Id = Guid.CreateVersion7(),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = "TOUR-2026",
            Title = title,
            Slug = slug,
            IsPublished = true,
            Images = [],
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

}
