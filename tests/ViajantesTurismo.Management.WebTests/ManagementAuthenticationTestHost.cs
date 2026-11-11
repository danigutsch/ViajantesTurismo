using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PandaBear.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Service provider hosting management authentication registrations for tests.</summary>
internal sealed class ManagementAuthenticationTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private ManagementAuthenticationTestHost(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>OpenID Connect options resolved for the default scheme.</summary>
    public OpenIdConnectOptions OpenIdConnectOptions => _provider
        .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
        .Get(OpenIdConnectDefaults.AuthenticationScheme);

    /// <summary>Cookie options resolved for the default scheme.</summary>
    public CookieAuthenticationOptions CookieOptions => _provider
        .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
        .Get(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>Server-side ticket store options.</summary>
    public ServerSideTicketStoreOptions TicketStoreOptions => _provider.GetRequiredService<IOptions<ServerSideTicketStoreOptions>>().Value;

    /// <summary>User token management options.</summary>
    public UserTokenManagementOptions UserTokenOptions => _provider.GetRequiredService<IOptions<UserTokenManagementOptions>>().Value;

    /// <summary>Registered session lock.</summary>
    public IUserSessionLock SessionLock => _provider.GetRequiredService<IUserSessionLock>();

    /// <summary>Registered user token manager.</summary>
    public UserTokenManager UserTokenManager => _provider.GetRequiredService<UserTokenManager>();

    /// <summary>Configured authorization options.</summary>
    public AuthorizationOptions AuthorizationOptions => _provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

    /// <summary>Resolves the registered advisory lock data source so its pool metrics are reported.</summary>
    public void ResolveAdvisoryLockDataSource()
    {
        _ = _provider.GetRequiredService<Npgsql.NpgsqlDataSource>();
    }

    /// <summary>Builds a host from the configuration and environment.</summary>
    public static ManagementAuthenticationTestHost Create(IConfiguration configuration, TestHostEnvironment environment)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagementAuthentication(configuration, environment);
        return new ManagementAuthenticationTestHost(services.BuildServiceProvider());
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }
}
