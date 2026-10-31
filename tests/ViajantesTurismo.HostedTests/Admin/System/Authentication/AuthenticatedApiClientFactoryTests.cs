using PandaBear.Testing;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using System.Net;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.System.Authentication;

[Trait(TestTraitNames.CategoryName, TestTraits.AuthenticationCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[TestScope(TestScope.System)]
public sealed class AuthenticatedApiClientFactoryTests(AspireSystemTestFixture fixture) : IClassFixture<AspireSystemTestFixture>
{
    [Fact]
    public async Task Admin_client_factory_returns_independently_owned_authenticated_clients()
    {
        // Arrange
        using var firstClient = await fixture.CreateApiClient(TestContext.Current.CancellationToken);
        using var secondClient = await fixture.CreateApiClient(TestContext.Current.CancellationToken);

        // Act
        firstClient.Dispose();
        using var response = await secondClient.GetAsync(
            new Uri("/api/v1/bookings", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        firstClient.ShouldNotBeSameAs(secondClient);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
