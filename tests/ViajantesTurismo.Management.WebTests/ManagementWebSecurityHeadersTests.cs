using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using System.Net;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementWebSecurityHeadersTests
{
    [Fact]
    public async Task Management_web_security_header_middleware_emits_critical_headers()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseManagementWebSecurityHeaders();
                    app.Run(static context => Results.Ok().ExecuteAsync(context));
                }))
            .StartAsync(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem().ShouldContain("connect-src 'self' ws: wss:", StringComparison.Ordinal);
        response.Headers.GetValues("X-Frame-Options").ShouldHaveSingleItem().ShouldBe("DENY");
        response.Headers.GetValues("Referrer-Policy").ShouldHaveSingleItem().ShouldBe("no-referrer");
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        response.Headers.GetValues("Permissions-Policy").ShouldHaveSingleItem().ShouldContain("geolocation=()", StringComparison.Ordinal);
    }
}
