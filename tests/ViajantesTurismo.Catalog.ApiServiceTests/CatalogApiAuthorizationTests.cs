using System.Net.Http.Json;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Verifies the authorization boundary between management and public Catalog endpoints.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiAuthorizationTests
{
    [Fact]
    public async Task Management_catalog_endpoint_rejects_anonymous_requests()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_a_token_from_an_untrusted_issuer()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureClientWithUntrustedIssuer(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_a_token_for_another_audience()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureClientWithWrongAudience(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_an_authenticated_role_without_permissions()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Guest");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Management_catalog_read_endpoint_accepts_supported_roles(string role)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Guest", HttpStatusCode.Forbidden)]
    public async Task Management_media_preview_rejects_unauthorized_roles(string? role, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/preview/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Management_media_preview_accepts_catalog_read_roles(string role)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/preview/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/v1/public/catalog/tours", HttpStatusCode.OK)]
    [InlineData("/api/v1/public/catalog/tours/missing", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/public/catalog/content/missing", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/public/catalog/media/1d02ec44-41b5-4d3a-878b-89f53261a803/640/jpg", HttpStatusCode.NotFound)]
    public async Task Public_catalog_endpoints_allow_anonymous_requests(string path, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri(path, UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData("PUT", "/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/presentation")]
    [InlineData("PUT", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-review")]
    [InlineData("POST", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-draft")]
    [InlineData("PUT", "/api/v1/catalog/public-content/home.hero")]
    public async Task Management_catalog_mutation_endpoints_reject_anonymous_requests(string method, string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(new { })
        };

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("PUT", "/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/presentation")]
    [InlineData("PUT", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-review")]
    [InlineData("POST", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-draft")]
    [InlineData("PUT", "/api/v1/catalog/public-content/home.hero")]
    public async Task Management_catalog_mutation_endpoints_reject_an_authenticated_role_without_permissions(string method, string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Guest");
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(new { })
        };

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Admin", "PUT", "/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/presentation", HttpStatusCode.BadRequest)]
    [InlineData("Admin", "PUT", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-review", HttpStatusCode.NotFound)]
    [InlineData("Admin", "POST", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-draft", HttpStatusCode.NotFound)]
    [InlineData("Operator", "PUT", "/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/presentation", HttpStatusCode.BadRequest)]
    [InlineData("Operator", "PUT", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-review", HttpStatusCode.NotFound)]
    [InlineData("Operator", "POST", "/api/v1/catalog/media/images/1d02ec44-41b5-4d3a-878b-89f53261a803/accessibility-draft", HttpStatusCode.NotFound)]
    [InlineData("Admin", "PUT", "/api/v1/catalog/public-content/home.hero", HttpStatusCode.BadRequest)]
    [InlineData("Operator", "PUT", "/api/v1/catalog/public-content/home.hero", HttpStatusCode.BadRequest)]
    public async Task Management_catalog_mutation_endpoints_accept_supported_roles(
        string role,
        string method,
        string path,
        HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(new { })
        };

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Guest", HttpStatusCode.Forbidden)]
    public async Task Catalog_tour_image_upload_rejects_unauthorized_roles(string? role, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]), "file", "tour.png");
        content.Add(new StringContent("Tour image"), "altText");

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/images", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Catalog_tour_image_upload_accepts_catalog_write_roles(string role)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]), "file", "tour.png");
        content.Add(new StringContent("Tour image"), "altText");

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/v1/catalog/tours/1d02ec44-41b5-4d3a-878b-89f53261a803/images", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
