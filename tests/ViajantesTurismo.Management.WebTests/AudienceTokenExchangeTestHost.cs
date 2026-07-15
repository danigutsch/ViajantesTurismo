using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
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
            _provider.GetRequiredService<IDistributedCache>(),
            _provider.GetRequiredService<IDataProtectionProvider>(),
            _provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>(),
            TimeProvider.System)
        {
            InnerHandler = Backend
        };
    }

    public static AudienceTokenExchangeTestHost Create()
    {
        var tokenEndpoint = new RecordingAudienceTokenEndpointHandler();
        var backend = new RecordingAudienceTokenBackendHandler();
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddDataProtection();
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
        var sourceTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(sourceAccessToken));
        return string.Concat("management-audience-token:", audience, ':', WebEncoders.Base64UrlEncode(sourceTokenHash));
    }

    public Task StoreProtectedAudienceTokenEntry(
        string audience,
        string sourceAccessToken,
        string accessToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        var cacheKey = GetAudienceTokenCacheKey(audience, sourceAccessToken);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(expiresAt.ToUnixTimeMilliseconds());
            writer.Write(accessToken);
            writer.Flush();
        }

        var protector = _provider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("ViajantesTurismo.Management.Web.AudienceTokenStore.v1")
            .CreateProtector(cacheKey);
        return Cache.SetAsync(
            cacheKey,
            protector.Protect(stream.ToArray()),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            ct);
    }

    public ValueTask DisposeAsync()
    {
        return _provider.DisposeAsync();
    }
}
