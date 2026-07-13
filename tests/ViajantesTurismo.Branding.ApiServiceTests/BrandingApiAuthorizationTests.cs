using System.Text;
using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.ApiServiceTests;

/// <summary>
/// Verifies the authorization boundary between management and public Branding endpoints.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingApiAuthorizationTests
{
    [Fact]
    public async Task Management_branding_endpoint_rejects_anonymous_requests()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_branding_endpoint_rejects_a_token_from_an_untrusted_issuer()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureClientWithUntrustedIssuer(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_branding_endpoint_rejects_a_token_for_another_audience()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureClientWithWrongAudience(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_branding_endpoint_rejects_an_authenticated_role_without_permissions()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, "Guest");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Management_branding_read_endpoint_accepts_supported_roles(string role)
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Public_branding_endpoint_allows_anonymous_requests()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/branding", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Management_branding_write_endpoint_rejects_anonymous_requests()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        using var request = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_branding_write_endpoint_rejects_an_authenticated_role_without_permissions()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, "Guest");
        using var request = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Management_branding_write_endpoint_accepts_supported_roles(string role)
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, role);
        using var currentSettingsResponse = await client.GetAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var currentSettings = await currentSettingsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var request = new StringContent(currentSettings, Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/api/v1/branding/settings", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        currentSettingsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
