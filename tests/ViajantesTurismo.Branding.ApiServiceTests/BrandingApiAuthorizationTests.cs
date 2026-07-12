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
}
