using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Builds in-memory test hosts for the management web endpoints.</summary>
internal static class ManagementWebEndpointTestHost
{
    private const string AntiforgeryHeaderNameResponseHeader = "X-Test-Antiforgery-Header-Name";
    private const string AntiforgeryRequestTokenHeader = "X-Test-Antiforgery-Request-Token";
    private const string AntiforgeryTokenPath = "/_test/antiforgery";

    /// <summary>Writes the current request's CSP nonce for the security-header test.</summary>
    public static Task WriteCspNonce(HttpContext context) =>
        context.Response.WriteAsync(context.RequestServices.GetRequiredService<ICspNonce>().Value, context.RequestAborted);

    /// <summary>Starts a test host whose authentication always succeeds and records challenges and sign-outs.</summary>
    public static async Task<IHost> StartWithRecordingAuthentication(
        CancellationToken ct,
        ICatalogToursApiClient? catalogToursApi = null,
        IDocumentsApiClient? documentsApiClient = null,
        ICatalogGalleryOperationsApiClient? galleryOperationsApi = null,
        Action<object?>? observeEndpointResult = null)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(static services => services.AddTestRequestBindingDefaults())
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(CookieAuthenticationDefaults.AuthenticationScheme, null)
                        .AddScheme<AuthenticationSchemeOptions, RecordingAuthenticationHandler>(OpenIdConnectDefaults.AuthenticationScheme, null);
                    services.AddAuthorization();
                    services.AddAntiforgery();
                    services.AddCspNonce();
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

                    if (galleryOperationsApi is not null)
                    {
                        services.AddSingleton(galleryOperationsApi);
                    }
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseManagementWebSecurityHeaders();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseAntiforgery();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(AntiforgeryTokenPath, static (HttpContext context, IAntiforgery antiforgery) =>
                        {
                            var tokens = antiforgery.GetAndStoreTokens(context);
                            context.Response.Headers.Append(AntiforgeryHeaderNameResponseHeader, tokens.HeaderName);
                            context.Response.Headers.Append(AntiforgeryRequestTokenHeader, tokens.RequestToken);
                            return Results.NoContent();
                        }).AllowAnonymous();
                        if (observeEndpointResult is null)
                        {
                            endpoints.MapManagementWebEndpoints();
                        }
                        else
                        {
                            endpoints.MapGroup(string.Empty)
                                .AddEndpointFilter(async (context, next) =>
                                {
                                    var result = await next(context);
                                    observeEndpointResult(result);
                                    return result;
                                })
                                .MapManagementWebEndpoints();
                        }
                    });
                }))
            .StartAsync(ct);
    }

    /// <summary>Creates a POST request carrying a valid antiforgery cookie and token header.</summary>
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
