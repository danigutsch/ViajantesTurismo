using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using System.Net;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementWebEndpointTests
{
    [Fact]
    public async Task Management_endpoints_reject_anonymous_requests()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(AnonymousAuthenticationHandler.Scheme)
                        .AddScheme<AuthenticationSchemeOptions, AnonymousAuthenticationHandler>(AnonymousAuthenticationHandler.Scheme, null);
                    services.AddAuthorization();
                    services.AddRazorComponents()
                        .AddInteractiveServerComponents();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseAntiforgery();
                    app.UseEndpoints(static endpoints => endpoints.MapManagementWebEndpoints());
                }))
            .StartAsync(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

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

    [Fact]
    public async Task Login_rejects_a_backslash_network_path_return_url()
    {
        // Arrange
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/login?returnUrl=/%5Cattacker.example", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);
        var redirectUri = response.Headers.GetValues(RecordingAuthenticationHandler.ChallengeRedirectHeaderName).Single();

        // Assert
        redirectUri.ShouldBe("/");
    }

    [Fact]
    public async Task Logout_clears_the_local_session_before_signing_out_of_oidc()
    {
        // Arrange
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/logout", UriKind.Relative),
            content: null,
            Xunit.TestContext.Current.CancellationToken);
        var signOutSchemes = response.Headers.GetValues(RecordingAuthenticationHandler.SignOutSchemeHeaderName).ToArray();

        // Assert
        signOutSchemes.ShouldContain(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        signOutSchemes.ShouldContain(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
    }
}
