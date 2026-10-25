using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Verifies DCP isolation when multiple test AppHosts start concurrently.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
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
        var firstBrandingEndpoint = firstApp.GetEndpoint(ResourceNames.BrandingApi, "https");
        var secondBrandingEndpoint = secondApp.GetEndpoint(ResourceNames.BrandingApi, "https");
        using var firstClient = firstApp.CreateHttpClient(ResourceNames.Api);
        using var secondClient = secondApp.CreateHttpClient(ResourceNames.Api);
        using var firstBrandingClient = firstApp.CreateHttpClient(ResourceNames.BrandingApi);
        using var secondBrandingClient = secondApp.CreateHttpClient(ResourceNames.BrandingApi);
        using var firstResponse = await firstClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondResponse = await secondClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var firstBrandingResponse = await firstBrandingClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondBrandingResponse = await secondBrandingClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        // Assert
        firstEndpoint.ShouldNotBe(secondEndpoint);
        firstBrandingEndpoint.ShouldNotBe(secondBrandingEndpoint);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondResponse.IsSuccessStatusCode.ShouldBeTrue();
        firstBrandingResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondBrandingResponse.IsSuccessStatusCode.ShouldBeTrue();
    }
}
