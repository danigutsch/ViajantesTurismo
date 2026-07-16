using Duende.AccessTokenManagement.OpenIdConnect;
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

    public IUserTokenStore UserTokenStore => _provider.GetRequiredService<IUserTokenStore>();

    public ProtectedDistributedUserTokenStore ProtectedUserTokenStore => _provider.GetRequiredService<ProtectedDistributedUserTokenStore>();

    public AuthorizationOptions AuthorizationOptions => _provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

    public HttpClient CreateKeycloakTokenExchangeClient()
    {
        return _provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName);
    }

    public ManagementAuthenticationTestScope CreateUserTokenStoreSession()
    {
        return new ManagementAuthenticationTestScope(_provider.GetRequiredService<IServiceScopeFactory>());
    }

    public static ManagementAuthenticationTestHost Create(IConfiguration configuration, TestHostEnvironment environment)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagementAuthentication(configuration, environment);
        return new ManagementAuthenticationTestHost(services.BuildServiceProvider());
    }

    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }
}
