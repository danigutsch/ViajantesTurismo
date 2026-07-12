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
    [Fact]
    public async Task Concurrent_test_app_hosts_use_distinct_dynamic_api_endpoints()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var applications = await ConcurrentAspireTestApplications.Start(
            ct => AspireTestApplication.Start<ViajantesTurismo_AppHost>(
                [ResourceNames.Api],
                null,
                AppHostTestArguments.Create(),
                ct),
            cancellationToken);

        // Act
        var firstEndpoint = applications.First.GetEndpoint(ResourceNames.Api, "https");
        var secondEndpoint = applications.Second.GetEndpoint(ResourceNames.Api, "https");
        using var firstClient = applications.First.CreateHttpClient(ResourceNames.Api);
        using var secondClient = applications.Second.CreateHttpClient(ResourceNames.Api);
        using var firstResponse = await firstClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);
        using var secondResponse = await secondClient.GetAsync(new Uri("/health", UriKind.Relative), cancellationToken);

        // Assert
        firstEndpoint.ShouldNotBe(secondEndpoint);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue();
        secondResponse.IsSuccessStatusCode.ShouldBeTrue();
    }
}
