using Projects;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Verifies DCP isolation when multiple test AppHosts start concurrently.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
public sealed class ConcurrentAspireTestApplicationTests
{
    private static readonly TimeSpan ConcurrentResourceStartupTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Concurrent_test_app_hosts_use_distinct_dynamic_service_endpoints()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        string[] appHostArguments = [.. AppHostTestArguments.Create(), .. HostedProfile.Admin.ToArguments()];
        await using var applications = await ConcurrentAspireTestApplications.Start(
            ct => AspireTestApplication.Start<ViajantesTurismo_AppHost>(
                [ResourceNames.Api, ResourceNames.BrandingApi],
                ConcurrentResourceStartupTimeout,
                appHostArguments,
                ct),
            cancellationToken);

        // Act
        var firstEndpoint = applications.First.GetEndpoint(ResourceNames.Api, "https");
        var secondEndpoint = applications.Second.GetEndpoint(ResourceNames.Api, "https");
        var firstBrandingEndpoint = applications.First.GetEndpoint(ResourceNames.BrandingApi, "https");
        var secondBrandingEndpoint = applications.Second.GetEndpoint(ResourceNames.BrandingApi, "https");
        using var firstClient = applications.First.CreateHttpClient(ResourceNames.Api);
        using var secondClient = applications.Second.CreateHttpClient(ResourceNames.Api);
        using var firstBrandingClient = applications.First.CreateHttpClient(ResourceNames.BrandingApi);
        using var secondBrandingClient = applications.Second.CreateHttpClient(ResourceNames.BrandingApi);
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
