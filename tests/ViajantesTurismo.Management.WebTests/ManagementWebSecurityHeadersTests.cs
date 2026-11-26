using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore;
using SharedKernel.Testing;
using System.Net;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[TestScope(TestScope.Integration)]
public sealed class ManagementWebSecurityHeadersTests
{
    [Fact]
    public async Task Management_web_security_header_middleware_emits_critical_headers()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(static services => services.AddTestRequestBindingDefaults().AddCspNonce())
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

    [Fact]
    public async Task Management_web_security_header_middleware_overwrites_downstream_headers()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(static services => services.AddTestRequestBindingDefaults().AddCspNonce())
                .Configure(app =>
                {
                    app.UseManagementWebSecurityHeaders();
                    app.Run(static context =>
                    {
                        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
                        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                        context.Response.Headers["X-Content-Type-Options"] = "unsafe";
                        return Results.Ok().ExecuteAsync(context);
                    });
                }))
            .StartAsync(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem().ShouldContain("connect-src 'self' ws: wss:", StringComparison.Ordinal);
        response.Headers.GetValues("X-Frame-Options").ShouldHaveSingleItem().ShouldBe("DENY");
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
    }

    [Fact]
    public async Task Management_web_script_and_style_policies_admit_only_the_site_and_the_response_nonce()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(static services => services.AddTestRequestBindingDefaults().AddCspNonce())
                .Configure(app =>
                {
                    app.UseManagementWebSecurityHeaders();
                    app.Run(ManagementWebEndpointTestHost.WriteCspNonce);
                }))
            .StartAsync(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var first = await client.GetAsync(new Uri("/", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);
        using var second = await client.GetAsync(new Uri("/", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);

        // Assert
        var nonce = await first.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
        var policy = first.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem();
        var secondPolicy = second.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem();
        policy.ShouldContain($"script-src 'self' 'nonce-{nonce}';", StringComparison.Ordinal);
        policy.ShouldContain($"style-src 'self' 'nonce-{nonce}';", StringComparison.Ordinal);
        policy.ShouldNotContain("unsafe-inline", StringComparison.Ordinal);
        secondPolicy.ShouldNotBe(policy);
    }

    [Theory]
    [InlineData("/operations/gallery", "Admin", HttpStatusCode.OK)]
    [InlineData("/operations/gallery", "Operator", HttpStatusCode.Forbidden)]
    [InlineData("/operations/gallery", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/operations/messaging/catalog-delivery", "Admin", HttpStatusCode.OK)]
    [InlineData("/operations/messaging/catalog-delivery", "Operator", HttpStatusCode.Forbidden)]
    [InlineData("/operations/messaging/catalog-delivery", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/operations", "Admin", HttpStatusCode.OK)]
    [InlineData("/operations", "Operator", HttpStatusCode.Forbidden)]
    [InlineData("/operations", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/operations/messaging/outbox", "Admin", HttpStatusCode.OK)]
    [InlineData("/operations/document-maintenance", "anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("/operations/document-maintenance/drafts", "Admin", HttpStatusCode.OK)]
    [InlineData("/operations/document-maintenance/drafts", "Operator", HttpStatusCode.Forbidden)]
    [InlineData("/operations/document-maintenance/drafts", "anonymous", HttpStatusCode.Unauthorized)]
    public async Task Operations_views_use_no_store_for_admin_challenge_and_forbid(string path, string role, HttpStatusCode status)
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder.UseTestServer()
                .ConfigureServices(static services =>
                {
                    services.AddTestRequestBindingDefaults().AddCspNonce();
                    services.AddRouting();
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(CookieAuthenticationDefaults.AuthenticationScheme, null);
                    services.AddAuthorization();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseManagementWebSecurityHeaders();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(path, () => Results.Ok())
                            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
                        endpoints.MapGet("/operations-example", () => Results.Ok()).AllowAnonymous();
                    });
                }))
            .StartAsync(ct);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);

        // Act
        using var viewer = await client.GetAsync(new Uri(path, UriKind.Relative), ct);
        using var outside = await client.GetAsync(new Uri("/operations-example", UriKind.Relative), ct);
        var outsideIsNoStore = outside.Headers.CacheControl?.NoStore == true;

        // Assert
        viewer.StatusCode.ShouldBe(status);
        viewer.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        outsideIsNoStore.ShouldBeFalse();
    }
}
