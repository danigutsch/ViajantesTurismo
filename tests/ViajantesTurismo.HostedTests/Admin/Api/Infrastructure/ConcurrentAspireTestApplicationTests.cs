using System.Net;
using System.Net.Http.Headers;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

/// <summary>
/// Verifies DCP isolation when multiple test AppHosts start concurrently.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[TestHost(TestHost.Aspire)]
[TestScope(TestScope.Integration)]
public sealed class ConcurrentAspireTestApplicationTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Concurrent_test_app_hosts_use_distinct_endpoints_and_owned_data()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = shared.GetApplication(HostedProfile.Admin, "concurrent-a", cancellationToken);
        var second = shared.GetApplication(HostedProfile.Admin, "concurrent-b", cancellationToken);
        await Task.WhenAll(first, second);
        var firstApp = await first;
        var secondApp = await second;
        var authority = await shared.GetIdentityProviderEndpoint(cancellationToken);
        var password = shared.Configuration.ConformanceUserPassword;

        // Act
        var firstEndpoint = firstApp.GetEndpoint(ResourceNames.Api, "https");
        var secondEndpoint = secondApp.GetEndpoint(ResourceNames.Api, "https");
        var firstCatalogEndpoint = firstApp.GetEndpoint(ResourceNames.CatalogApi, "https");
        var secondCatalogEndpoint = secondApp.GetEndpoint(ResourceNames.CatalogApi, "https");
        using var firstClient = firstApp.CreateHttpClient(ResourceNames.Api);
        using var secondClient = secondApp.CreateHttpClient(ResourceNames.Api);
        using var firstCatalogClient = firstApp.CreateHttpClient(ResourceNames.CatalogApi);
        using var secondCatalogClient = secondApp.CreateHttpClient(ResourceNames.CatalogApi);
        var firstToken = await KeycloakConformanceClient.RequestAccessToken(
            authority, password, [ApiAudienceNames.Admin], cancellationToken, realm: shared.Realm("concurrent-a"));
        var secondToken = await KeycloakConformanceClient.RequestAccessToken(
            authority, password, [ApiAudienceNames.Admin], cancellationToken, realm: shared.Realm("concurrent-b"));
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstToken);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondToken);

        using var firstResponse = await firstClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondResponse = await secondClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var firstCatalogResponse = await firstCatalogClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondCatalogResponse = await secondCatalogClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        var customer = await firstClient.CreateTestCustomer("First stack", "Isolation", cancellationToken);
        var customerUri = new Uri($"/api/v1/customers/{customer.Id}", UriKind.Relative);
        using var ownerResponse = await firstClient.GetAsync(customerUri, cancellationToken);
        using var siblingResponse = await secondClient.GetAsync(customerUri, cancellationToken);

        // Assert
        firstEndpoint.ShouldNotBe(secondEndpoint);
        firstCatalogEndpoint.ShouldNotBe(secondCatalogEndpoint);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondResponse.IsSuccessStatusCode.ShouldBeTrue();
        firstCatalogResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondCatalogResponse.IsSuccessStatusCode.ShouldBeTrue();
        ownerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        siblingResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
