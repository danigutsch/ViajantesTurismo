using PandaBear.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

/// <summary>
/// Verifies DCP isolation when multiple test AppHosts start concurrently.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[TestScope(TestScope.Integration)]
public sealed class ConcurrentAspireTestApplicationTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Concurrent_test_app_hosts_use_distinct_dynamic_service_endpoints()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = shared.GetApplication(HostedProfile.Admin, "concurrent-a", cancellationToken);
        var second = shared.GetApplication(HostedProfile.Admin, "concurrent-b", cancellationToken);
        await Task.WhenAll(first, second);
        var firstApp = await first;
        var secondApp = await second;

        // Act
        var firstEndpoint = firstApp.GetEndpoint(ResourceNames.Api, "https");
        var secondEndpoint = secondApp.GetEndpoint(ResourceNames.Api, "https");
        var firstCatalogEndpoint = firstApp.GetEndpoint(ResourceNames.CatalogApi, "https");
        var secondCatalogEndpoint = secondApp.GetEndpoint(ResourceNames.CatalogApi, "https");
        using var firstClient = firstApp.CreateHttpClient(ResourceNames.Api);
        using var secondClient = secondApp.CreateHttpClient(ResourceNames.Api);
        using var firstCatalogClient = firstApp.CreateHttpClient(ResourceNames.CatalogApi);
        using var secondCatalogClient = secondApp.CreateHttpClient(ResourceNames.CatalogApi);
        using var firstResponse = await firstClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondResponse = await secondClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var firstCatalogResponse = await firstCatalogClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondCatalogResponse = await secondCatalogClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        // Assert
        firstEndpoint.ShouldNotBe(secondEndpoint);
        firstCatalogEndpoint.ShouldNotBe(secondCatalogEndpoint);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondResponse.IsSuccessStatusCode.ShouldBeTrue();
        firstCatalogResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondCatalogResponse.IsSuccessStatusCode.ShouldBeTrue();
    }
}
