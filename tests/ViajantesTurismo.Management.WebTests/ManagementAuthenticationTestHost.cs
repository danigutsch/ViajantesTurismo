using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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

    /// <summary>Registered ticket store.</summary>
    public ITicketStore TicketStore => _provider.GetRequiredService<ITicketStore>();

    /// <summary>Registered user token store.</summary>
    public IUserTokenStore UserTokenStore => _provider.GetRequiredService<IUserTokenStore>();

    /// <summary>Registered protected distributed user token store.</summary>
    public ProtectedDistributedUserTokenStore ProtectedUserTokenStore => _provider.GetRequiredService<ProtectedDistributedUserTokenStore>();

    /// <summary>Configured authorization options.</summary>
    public AuthorizationOptions AuthorizationOptions => _provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

    /// <summary>Resolves the registered advisory lock data source so its pool metrics are reported.</summary>
    public void ResolveAdvisoryLockDataSource()
    {
        _ = _provider.GetRequiredService<Npgsql.NpgsqlDataSource>();
    }

    /// <summary>Creates the HTTP client used for Keycloak token exchange.</summary>
    public HttpClient CreateKeycloakTokenExchangeClient()
    {
        return _provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName);
    }

    /// <summary>Creates a scope-bound session for resolving user token stores.</summary>
    public ManagementAuthenticationTestScope CreateUserTokenStoreSession()
    {
        return new ManagementAuthenticationTestScope(_provider.GetRequiredService<IServiceScopeFactory>());
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
