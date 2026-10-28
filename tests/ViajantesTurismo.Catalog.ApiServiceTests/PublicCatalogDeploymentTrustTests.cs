using PandaBear.Testing;
using ViajantesTurismo.Catalog.ApiService;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class PublicCatalogDeploymentTrustTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_trusted_ingress_hops_preserve_visitor_isolation_across_frontends(bool trustNetwork)
    {
        // Arrange
        var frontendConfiguration = new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:ForwardLimit"] = "2"
        };
        if (trustNetwork)
        {
            frontendConfiguration["Security:ForwardedHeaders:KnownNetworks:0"] = "192.0.2.0/24";
        }
        else
        {
            frontendConfiguration["Security:ForwardedHeaders:KnownProxies:0"] = "192.0.2.10";
            frontendConfiguration["Security:ForwardedHeaders:KnownProxies:1"] = "192.0.2.20";
        }

        var catalogConfiguration = new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
            ["Security:ForwardedHeaders:ForwardLimit"] = "1"
        };
        await using var proxy = new PublicCatalogProxyTestScope(
            frontendConfiguration: frontendConfiguration, catalogConfiguration: catalogConfiguration);
        using var firstFrontend = proxy.CreateVisitor(IPAddress.Parse("192.0.2.10"));
        using var secondFrontend = proxy.CreateVisitor(IPAddress.Parse("192.0.2.10"), frontendReplica: 1);
        using var thirdFrontend = proxy.CreateVisitor(IPAddress.Parse("192.0.2.10"), frontendReplica: 2);
        var path = new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative);
        // The network case also makes the limit observable when another address matches the allowlist.
        var attackingVisitor = trustNetwork ? "192.0.2.100" : "203.0.113.10";
        var unrelatedVisitor = trustNetwork ? "192.0.2.101" : "203.0.113.20";

        // Act
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{requestNumber}, {attackingVisitor}, 192.0.2.20");
            var frontend = requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit / 2 ? firstFrontend : secondFrontend;
            using var response = await frontend.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        using var exhaustedRequest = new HttpRequestMessage(HttpMethod.Get, path);
        exhaustedRequest.Headers.Add("X-Forwarded-For", $"{unrelatedVisitor}, {attackingVisitor}, 192.0.2.20");
        using var exhaustedResponse = await thirdFrontend.SendAsync(exhaustedRequest, TestContext.Current.CancellationToken);
        using var unrelatedRequest = new HttpRequestMessage(HttpMethod.Get, path);
        unrelatedRequest.Headers.Add("X-Forwarded-For", $"{attackingVisitor}, {unrelatedVisitor}, 192.0.2.20");
        using var unrelatedResponse = await thirdFrontend.SendAsync(unrelatedRequest, TestContext.Current.CancellationToken);

        // Assert
        exhaustedResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        unrelatedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_untrusted_intermediate_hop_cannot_select_a_different_visitor()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:KnownProxies:0"] = "192.0.2.10",
            ["Security:ForwardedHeaders:ForwardLimit"] = "2"
        };
        await using var proxy = new PublicCatalogProxyTestScope(frontendConfiguration: configuration);
        using var frontend = proxy.CreateVisitor(IPAddress.Parse("192.0.2.10"));
        var path = new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative);
        for (var requestNumber = 0; requestNumber < 120; requestNumber++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{requestNumber}, 203.0.113.10");
            using var response = await frontend.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // Act
        using var exhaustedRequest = new HttpRequestMessage(HttpMethod.Get, path);
        exhaustedRequest.Headers.Add("X-Forwarded-For", "203.0.113.20, 203.0.113.10");
        using var exhaustedResponse = await frontend.SendAsync(exhaustedRequest, TestContext.Current.CancellationToken);
        using var unrelatedRequest = new HttpRequestMessage(HttpMethod.Get, path);
        unrelatedRequest.Headers.Add("X-Forwarded-For", "203.0.113.10, 203.0.113.20");
        using var unrelatedResponse = await frontend.SendAsync(unrelatedRequest, TestContext.Current.CancellationToken);

        // Assert
        exhaustedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        unrelatedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Frontend_admission_allowances_are_process_local()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        var address = IPAddress.Parse("203.0.113.10");
        using var firstFrontend = proxy.CreateVisitor(address);
        using var secondFrontend = proxy.CreateVisitor(address, frontendReplica: 1);
        var path = new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative);
        for (var requestNumber = 0; requestNumber < 120; requestNumber++)
        {
            using var response = await firstFrontend.GetAsync(path, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // Act
        using var exhaustedResponse = await firstFrontend.GetAsync(path, TestContext.Current.CancellationToken);
        using var otherProcessResponse = await secondFrontend.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        exhaustedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherProcessResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
