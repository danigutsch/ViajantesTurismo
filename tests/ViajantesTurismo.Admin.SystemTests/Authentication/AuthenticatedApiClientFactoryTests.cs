using System.Net;
using ViajantesTurismo.Admin.SystemTests.Infrastructure;

namespace ViajantesTurismo.Admin.SystemTests.Authentication;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.AuthenticationCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.SystemScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
public sealed class AuthenticatedApiClientFactoryTests(AspireSystemTestFixture fixture)
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
