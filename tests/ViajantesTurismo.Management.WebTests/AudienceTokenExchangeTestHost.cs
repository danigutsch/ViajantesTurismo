using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class AudienceTokenExchangeTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private AudienceTokenExchangeTestHost(
        ServiceProvider provider,
        RecordingAudienceTokenEndpointHandler tokenEndpoint,
        RecordingAudienceTokenBackendHandler backend)
    {
        _provider = provider;
        TokenEndpoint = tokenEndpoint;
        Backend = backend;
    }

    public RecordingAudienceTokenEndpointHandler TokenEndpoint { get; }

    public RecordingAudienceTokenBackendHandler Backend { get; }

    public IDistributedCache Cache => _provider.GetRequiredService<IDistributedCache>();

    public KeycloakAudienceTokenExchangeHandler CreateHandler(string audience)
    {
        return CreateHandler(audience, ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-default"));
    }

    public KeycloakAudienceTokenExchangeHandler CreateHandler(string audience, ClaimsPrincipal user)
    {
        return new KeycloakAudienceTokenExchangeHandler(
            audience,
            _provider.GetRequiredService<IHttpClientFactory>(),
            _provider.GetRequiredService<ProtectedDistributedAudienceTokenStore>(),
            _provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>(),
            new FixedUserAccessor(user),
            _provider.GetRequiredService<ProtectedDistributedUserTokenStore>(),
            TimeProvider.System)
        {
            InnerHandler = Backend
        };
    }

    public static AudienceTokenExchangeTestHost Create(IDistributedCache? cache = null)
    {
        var tokenEndpoint = new RecordingAudienceTokenEndpointHandler();
        var backend = new RecordingAudienceTokenBackendHandler();
        var services = new ServiceCollection();
        if (cache is null)
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddSingleton(cache);
        }
        services.AddDataProtection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ProtectedDistributedAudienceTokenStore>();
        services.AddSingleton(serviceProvider =>
            ProtectedDistributedUserTokenStore.CreateForTesting(
                serviceProvider.GetRequiredService<IDistributedCache>(),
                serviceProvider.GetRequiredService<IDataProtectionProvider>(),
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.AddOptions();
        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = "web-app";
            options.ClientSecret = "client-secret";
            options.Configuration = new OpenIdConnectConfiguration
            {
                TokenEndpoint = "https://identity.example.test/realms/viajantes/protocol/openid-connect/token"
            };
        });
        services.AddHttpClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);

        var provider = services.BuildServiceProvider();

        return new AudienceTokenExchangeTestHost(provider, tokenEndpoint, backend);
    }

    public static string GetAudienceTokenCacheKey(string audience, ClaimsPrincipal user)
    {
        return ProtectedDistributedAudienceTokenStore.GetCacheKey(audience, ManagementTokenSession.From(user));
    }

    public Task StoreProtectedAudienceTokenEntry(
        string audience,
        ClaimsPrincipal user,
        string sourceAccessToken,
        string accessToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        return _provider.GetRequiredService<ProtectedDistributedAudienceTokenStore>()
            .Store(audience, ManagementTokenSession.From(user), sourceAccessToken, accessToken, expiresAt, ct);
    }

    public Task RevokeUserTokenSession(ClaimsPrincipal user, CancellationToken ct)
    {
        return _provider.GetRequiredService<ProtectedDistributedUserTokenStore>().ClearAll(user, ct);
    }

    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }

    private sealed class FixedUserAccessor(ClaimsPrincipal user) : IUserAccessor
    {
        public Task<ClaimsPrincipal> GetCurrentUserAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(user);
        }
    }
}
