using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
    public async Task Catalog_public_reads_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        for (var requestNumber = 0; requestNumber < 60; requestNumber++)
        {
            using var allowedResponse = await client.GetAsync(new Uri("/public/catalog/theme", UriKind.Relative), TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var limitedResponse = await client.GetAsync(new Uri("/public/catalog/theme", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
