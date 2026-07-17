using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing;
using System.Net;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementWebEndpointTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/catalog/media/images/6db0b8be-e4e8-4500-a398-b44e7709a640/preview/640/jpg")]
    public async Task Management_endpoints_reject_anonymous_requests(string path)
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(AnonymousAuthenticationHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, AnonymousAuthenticationHandler>(AnonymousAuthenticationHandler.SchemeName, null);
                    services.AddAuthorization();
                    services.AddAntiforgery();
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
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_media_preview_forwards_authenticated_request_to_catalog()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        using var catalogResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var catalogApi = new FakeCatalogToursApiClient
        {
            Media = new PublicMediaObjectResponse(
                catalogResponse,
                new MemoryStream("image"u8.ToArray()),
                "image/jpeg")
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            catalogApi);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/catalog/media/images/{imageId}/preview/640/jpg", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        content.ShouldBe("image"u8.ToArray());
        catalogApi.LastMediaId.ShouldBe(imageId);
        catalogApi.LastMediaWidth.ShouldBe(640);
        catalogApi.LastMediaFormat.ShouldBe("jpg");
    }

    [Fact]
    public async Task Management_media_preview_returns_non_cacheable_service_unavailable_when_catalog_request_fails()
    {
        // Arrange
        var catalogApi = new FakeCatalogToursApiClient { ThrowOnMediaPreview = true };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            catalogApi);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/catalog/media/images/6db0b8be-e4e8-4500-a398-b44e7709a640/preview/640/jpg", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
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
        using var request = await ManagementWebEndpointTestHost.CreateAntiforgeryPost(
            client,
            "/logout",
            Xunit.TestContext.Current.CancellationToken);
        using var response = await client.SendAsync(request, Xunit.TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var signOutSchemes = response.Headers.GetValues(RecordingAuthenticationHandler.SignOutSchemeHeaderName).ToArray();

        // Assert
        signOutSchemes.ShouldContain(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        signOutSchemes.ShouldContain(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Fact]
    public async Task Logout_rejects_requests_without_an_antiforgery_token()
    {
        // Arrange
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(Xunit.TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.PostAsync(
            new Uri("/logout", UriKind.Relative),
            content: null,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
