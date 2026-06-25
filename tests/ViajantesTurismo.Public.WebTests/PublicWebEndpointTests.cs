using TestTraits = SharedKernel.Testing.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraits.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraits.HostName, TestTraits.TestServerHost)]
public sealed class PublicWebEndpointTests
{
    [Fact]
    public async Task Root_Returns_Public_Landing_Page()
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
    public async Task Root_Returns_Published_Tours_When_Catalog_Loads()
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
    public async Task Root_Returns_Unavailable_Message_When_Catalog_Fails()
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
    public async Task Public_Ssr_Routes_Return_Expected_Content(string path, string expectedHeading)
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
    public async Task Public_Tour_List_Returns_Published_Tours_When_Catalog_Loads()
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
    public async Task Public_Tour_List_Returns_Unavailable_Message_When_Catalog_Fails()
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
    public async Task Public_Tour_Details_Returns_Tour_Content_When_Catalog_Loads()
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
    public async Task Public_Tour_Details_Returns_Unavailable_When_Catalog_Fails()
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
    public async Task Public_Tour_Details_Returns_Not_Found_When_Tour_Is_Not_Published()
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
    public async Task Default_Health_Endpoint_Returns_Success(string path)
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
    public async Task Error_Endpoint_Returns_Problem_Response()
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
    public async Task Production_Root_Returns_Public_Landing_Page()
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
    public async Task Production_Default_Health_Endpoint_Is_Not_Exposed(string path)
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

    private static class PublicWebEndpointTestsHelpers
    {
        public static CatalogTourDto CreateTour(string slug, string title)
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

        public static WebApplicationFactory<IPublicWebAssemblyMarker> CreateFactory(
                FakePublicCatalogApiClient? catalogApiClient = null,
                string? environment = null)
        {
            return WebApplicationTestHost.Create<IPublicWebAssemblyMarker>(
                environment,
                services =>
                {
                    services.RemoveAll<IPublicCatalogApiClient>();
                    services.AddSingleton<IPublicCatalogApiClient>(catalogApiClient ?? new FakePublicCatalogApiClient());
                });
        }
    }
}
