using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using System.Net;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementWebEndpointTests
{
    [Fact]
    public async Task Robots_txt_disallows_management_crawling()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRazorComponents()
                    .AddInteractiveServerComponents())
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(static endpoints => endpoints.MapManagementWebEndpoints());
                }))
            .StartAsync(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(new Uri("/robots.txt", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nDisallow: /");
    }
}
