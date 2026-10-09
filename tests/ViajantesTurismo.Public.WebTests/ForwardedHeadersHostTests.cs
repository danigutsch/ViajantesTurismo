using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class ForwardedHeadersHostTests
{
    [Fact]
    public async Task Trusted_loopback_forwarded_https_request_avoids_https_redirect()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            ["https_port"] = "443",
            ["Security:ForwardedHeaders:KnownProxies:0"] = "127.0.0.1"
        };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(configuration: configuration);
        using var client = new HttpClient(factory.Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = IPAddress.Loopback))
        {
            BaseAddress = new Uri("http://localhost")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/alive", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Configured_untrusted_loopback_forwarded_https_request_remains_redirected()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            ["https_port"] = "443",
            ["Security:ForwardedHeaders:KnownProxies:0"] = "192.0.2.1"
        };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(configuration: configuration);
        using var client = new HttpClient(factory.Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = IPAddress.Loopback))
        {
            BaseAddress = new Uri("http://localhost")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/alive", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
    }

    [Fact]
    public async Task Global_forwarding_switch_without_trusted_proxies_does_not_trust_remote_clients()
    {
        // Arrange
        var configuration = new Dictionary<string, string?>
        {
            ["https_port"] = "443",
            ["ForwardedHeaders_Enabled"] = bool.TrueString
        };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(configuration: configuration);
        using var client = new HttpClient(factory.Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10")))
        {
            BaseAddress = new Uri("http://localhost")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/alive", UriKind.Relative));
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
    }
}
