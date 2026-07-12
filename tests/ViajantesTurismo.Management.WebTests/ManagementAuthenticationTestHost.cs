using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ManagementAuthenticationTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private ManagementAuthenticationTestHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    public OpenIdConnectOptions OpenIdConnectOptions => _provider
        .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
        .Get(OpenIdConnectDefaults.AuthenticationScheme);

    public CookieAuthenticationOptions CookieOptions => _provider
        .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
        .Get(CookieAuthenticationDefaults.AuthenticationScheme);

    public ITicketStore TicketStore => _provider.GetRequiredService<ITicketStore>();

    public AuthorizationOptions AuthorizationOptions => _provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

    public static ManagementAuthenticationTestHost Create(IConfiguration configuration, TestHostEnvironment environment)
    {
        var services = new ServiceCollection();
        services.AddManagementAuthentication(configuration, environment);
        return new ManagementAuthenticationTestHost(services.BuildServiceProvider());
    }

    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }
}
