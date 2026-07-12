using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

internal static class ManagementWebEndpointTestHost
{
    public static async Task<IHost> StartWithRecordingAuthentication(CancellationToken ct)
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
            .StartAsync(ct);
    }
}
