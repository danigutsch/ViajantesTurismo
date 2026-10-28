using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBear.Blazor;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components;
using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>
/// Hosts the real Management Web Razor components on a loopback Kestrel port with synthetic authentication,
/// so a browser can drive them against the API clients registered by the caller.
/// </summary>
internal sealed class ManagementWebBrowserHost : IAsyncDisposable
{
    private readonly WebApplication application;

    private ManagementWebBrowserHost(WebApplication application, Uri baseUri)
    {
        this.application = application;
        BaseUri = baseUri;
    }

    /// <summary>Gets the loopback address the host listens on.</summary>
    public Uri BaseUri
    {
        get;
    }

    /// <summary>Starts the host with the given service registrations layered over the page infrastructure.</summary>
    public static async Task<ManagementWebBrowserHost> Start(IServiceCollection registrations, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(App).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddAuthentication(AlwaysAuthenticatedHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AlwaysAuthenticatedHandler>(AlwaysAuthenticatedHandler.SchemeName, null);
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddMetrics();
        builder.Services.AddUiFailureReporting();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddScoped<CustomerCreationState>();
        builder.Services.AddScoped<ICountryService, CountryService>();
        foreach (var registration in registrations)
        {
            builder.Services.Add(registration);
        }

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets().AllowAnonymous();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireAuthorization();

        await app.StartAsync(ct);
        var address = (app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException("The server publishes no addresses.")).Addresses.Single();
        return new ManagementWebBrowserHost(app, new Uri(address));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await application.DisposeAsync();
    }
}
