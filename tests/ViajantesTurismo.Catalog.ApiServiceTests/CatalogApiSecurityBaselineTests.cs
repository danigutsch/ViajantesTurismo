using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiSecurityBaselineTests
{
    [Fact]
    public async Task Catalog_api_allows_configured_cors_origin()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Cors:AllowedOrigins:0"] = " https://public.example\u0007 "
            })
            .Build();
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services.AddCatalogSecurityBaseline(configuration))
                .Configure(app =>
                {
                    app.UseCors(CatalogSecurityBaseline.CorsPolicyName);
                    app.Run(static context => Results.Ok().ExecuteAsync(context));
                }))
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.Add("Origin", "https://public.example");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldHaveSingleItem().ShouldBe("https://public.example");
    }

    [Fact]
    public async Task Catalog_api_omits_cors_headers_for_denied_origins()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Cors:AllowedOrigins:0"] = "https://public.example"
            })
            .Build();
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services.AddCatalogSecurityBaseline(configuration))
                .Configure(app =>
                {
                    app.UseCors(CatalogSecurityBaseline.CorsPolicyName);
                    app.Run(static context => Results.Ok().ExecuteAsync(context));
                }))
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.Add("Origin", "https://evil.example");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_api_omits_cors_headers_when_no_origins_are_configured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services.AddCatalogSecurityBaseline(configuration))
                .Configure(app =>
                {
                    app.UseCors(CatalogSecurityBaseline.CorsPolicyName);
                    app.Run(static context => Results.Ok().ExecuteAsync(context));
                }))
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.Add("Origin", "https://public.example");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_public_reads_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        for (var requestNumber = 0; requestNumber < 60; requestNumber++)
        {
            using var allowedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var limitedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void Forwarded_headers_configuration_uses_trusted_proxy_entries()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KnownProxies:0"] = "10.0.0.10",
                ["KnownNetworks:0"] = "10.1.0.0/16"
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        // Act
        options.ConfigureTrustedForwardedHeaders(configuration);

        // Assert
        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        options.ForwardLimit.ShouldBe(1);
        options.KnownProxies.ShouldHaveSingleItem().ToString().ShouldBe("10.0.0.10");
        var knownNetwork = options.KnownIPNetworks.ShouldHaveSingleItem();
        knownNetwork.BaseAddress.ToString().ShouldBe("10.1.0.0");
        knownNetwork.PrefixLength.ShouldBe(16);
    }

    [Fact]
    public void Forwarded_headers_configuration_uses_configured_forward_limit()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KnownNetworks:0"] = "10.1.0.0/16",
                ["ForwardLimit"] = "3"
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        // Act
        options.ConfigureTrustedForwardedHeaders(configuration);

        // Assert
        options.ForwardLimit.ShouldBe(3);
    }

    [Theory]
    [InlineData("not-a-network")]
    [InlineData("10.0.0.0/999")]
    [InlineData("0.0.0.0/0")]
    [InlineData("::/0")]
    public void Forwarded_headers_configuration_rejects_invalid_networks(string network)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KnownNetworks:0"] = network
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        // Act
        Action action = () => options.ConfigureTrustedForwardedHeaders(configuration);

        var exception = action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("KnownNetworks", StringComparison.Ordinal);
    }

    [Fact]
    public void Forwarded_headers_configuration_rejects_invalid_proxies()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KnownProxies:0"] = "not-an-ip"
            })
            .Build();
        var options = new ForwardedHeadersOptions();

        // Act
        Action action = () => options.ConfigureTrustedForwardedHeaders(configuration);

        var exception = action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("KnownProxies", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Forwarded_headers_apply_trusted_client_ip_before_endpoint_execution()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownProxies.Add(IPAddress.Loopback);
                }))
                .Configure(app =>
                {
                    app.UseForwardedHeaders();
                    app.Run(static context => context.Response.WriteAsync(context.Connection.RemoteIpAddress?.ToString() ?? "missing"));
                }))
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("203.0.113.7");
    }
}
