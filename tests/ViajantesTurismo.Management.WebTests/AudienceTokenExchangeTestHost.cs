using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Test host wiring the Keycloak audience token exchange handler to recording token endpoint and backend handlers over an in-memory token cache.</summary>
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

    /// <summary>Records the requests the host's fake token endpoint received.</summary>
    public RecordingAudienceTokenEndpointHandler TokenEndpoint
    {
        get;
    }

    /// <summary>Records the requests that reached the fake backend behind the exchange handler.</summary>
    public RecordingAudienceTokenBackendHandler Backend
    {
        get;
    }

    /// <summary>Distributed cache backing the protected audience and user token stores.</summary>
    public IDistributedCache Cache => _provider.GetRequiredService<IDistributedCache>();

    /// <summary>Creates an exchange handler for <paramref name="audience"/> acting as a default test user session.</summary>
    public KeycloakAudienceTokenExchangeHandler CreateHandler(string audience)
    {
        return CreateHandler(audience, ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-default"));
    }

    /// <summary>Creates an exchange handler for <paramref name="audience"/> acting as <paramref name="user"/>.</summary>
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

    /// <summary>Creates a host, optionally using the given cache and customizing the OpenID Connect options.</summary>
    public static AudienceTokenExchangeTestHost Create(
        IDistributedCache? cache = null,
        Action<OpenIdConnectOptions>? configureOptions = null)
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
            configureOptions?.Invoke(options);
        });
        services.AddHttpClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);

        var provider = services.BuildServiceProvider();

        return new AudienceTokenExchangeTestHost(provider, tokenEndpoint, backend);
    }

    /// <summary>Returns the cache key under which the audience token for <paramref name="user"/> is stored.</summary>
    public static string GetAudienceTokenCacheKey(string audience, ClaimsPrincipal user)
    {
        return ProtectedDistributedAudienceTokenStore.GetCacheKey(audience, ManagementTokenSession.From(user));
    }

    /// <summary>Stores a protected audience token entry for <paramref name="user"/>, bypassing the exchange flow.</summary>
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

    /// <summary>Clears every stored token for the user's session, simulating session revocation.</summary>
    public Task RevokeUserTokenSession(ClaimsPrincipal user, CancellationToken ct)
    {
        return _provider.GetRequiredService<ProtectedDistributedUserTokenStore>().ClearAll(user, ct);
    }

    /// <summary>Disposes the underlying service provider.</summary>
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
