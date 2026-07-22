using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

internal static class ManagementWebEndpointTestHost
{
    private const string AntiforgeryHeaderNameResponseHeader = "X-Test-Antiforgery-Header-Name";
    private const string AntiforgeryRequestTokenHeader = "X-Test-Antiforgery-Request-Token";
    private const string AntiforgeryTokenPath = "/_test/antiforgery";

    public static async Task<IHost> StartWithRecordingAuthentication(
        CancellationToken ct,
        ICatalogToursApiClient? catalogToursApi = null,
        IDocumentsApiClient? documentsApiClient = null)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(CookieAuthenticationDefaults.AuthenticationScheme, null)
                        .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(OpenIdConnectDefaults.AuthenticationScheme, null);
                    services.AddAuthorization();
                    services.AddAntiforgery();
                    services.AddRazorComponents()
                        .AddInteractiveServerComponents();
                    if (catalogToursApi is not null)
                    {
                        services.AddSingleton(catalogToursApi);
                    }

                    if (documentsApiClient is not null)
                    {
                        services.AddSingleton(documentsApiClient);
                    }
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseAntiforgery();
                    app.UseEndpoints(static endpoints =>
                    {
                        endpoints.MapGet(AntiforgeryTokenPath, static (HttpContext context, IAntiforgery antiforgery) =>
                        {
                            var tokens = antiforgery.GetAndStoreTokens(context);
                            context.Response.Headers.Append(AntiforgeryHeaderNameResponseHeader, tokens.HeaderName);
                            context.Response.Headers.Append(AntiforgeryRequestTokenHeader, tokens.RequestToken);
                            return Results.NoContent();
                        }).AllowAnonymous();
                        endpoints.MapManagementWebEndpoints();
                    });
                }))
            .StartAsync(ct);
    }

    public static async Task<HttpRequestMessage> CreateAntiforgeryPost(HttpClient client, string path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var tokenResponse = await client.GetAsync(new Uri(AntiforgeryTokenPath, UriKind.Relative), ct);
        var headerName = tokenResponse.Headers.GetValues(AntiforgeryHeaderNameResponseHeader).Single();
        var requestToken = tokenResponse.Headers.GetValues(AntiforgeryRequestTokenHeader).Single();
        var cookie = tokenResponse.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add(headerName, requestToken);
        return request;
    }
}
