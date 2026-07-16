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
        return new KeycloakAudienceTokenExchangeHandler(
            audience,
            _provider.GetRequiredService<IHttpClientFactory>(),
            _provider.GetRequiredService<ProtectedDistributedAudienceTokenStore>(),
            _provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>(),
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

    public static string GetAudienceTokenCacheKey(string audience, string sourceAccessToken)
    {
        return ProtectedDistributedAudienceTokenStore.GetCacheKey(audience, sourceAccessToken);
    }

    public Task StoreProtectedAudienceTokenEntry(
        string audience,
        string sourceAccessToken,
        string accessToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        return _provider.GetRequiredService<ProtectedDistributedAudienceTokenStore>()
            .Store(audience, sourceAccessToken, accessToken, expiresAt, ct);
    }

    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }
}
