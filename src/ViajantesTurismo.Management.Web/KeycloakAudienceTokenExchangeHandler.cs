using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Exchanges the managed user access token for one Keycloak backend audience.
/// </summary>
internal sealed class KeycloakAudienceTokenExchangeHandler : DelegatingHandler
{
    private const string AudienceTokenCacheKeyPrefix = "management-audience-token:";
    private const string AudienceTokenProtectorPurpose = "ViajantesTurismo.Management.Web.AudienceTokenStore.v1";
    private static readonly TimeSpan RefreshBeforeExpiration = TimeSpan.FromMinutes(1);

    private readonly string _audience;
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<OpenIdConnectOptions> _openIdConnectOptions;
    private readonly TimeProvider _timeProvider;

    public KeycloakAudienceTokenExchangeHandler(
        string audience,
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        IOptionsMonitor<OpenIdConnectOptions> openIdConnectOptions,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(openIdConnectOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _audience = GetSupportedAudience(audience);
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(AudienceTokenProtectorPurpose);
        _openIdConnectOptions = openIdConnectOptions;
        _timeProvider = timeProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sourceAccessToken = GetSourceAccessToken(request);
        var accessToken = await GetCachedAccessToken(sourceAccessToken, cancellationToken)
            ?? await ExchangeAccessToken(sourceAccessToken, cancellationToken);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> ExchangeAccessToken(string sourceAccessToken, CancellationToken cancellationToken)
    {
        var options = _openIdConnectOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var clientId = options.ClientId;
        var clientSecret = options.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("The management token-exchange client is unavailable.");
        }

        var tokenEndpoint = await GetTokenEndpoint(options, cancellationToken);
        var tokenClient = _httpClientFactory.CreateClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName);
        var response = await tokenClient.RequestTokenExchangeTokenAsync(
            new TokenExchangeTokenRequest
            {
                Address = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                SubjectToken = sourceAccessToken,
                SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
                RequestedTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
                Audience = _audience,
                Scope = _audience
            },
            cancellationToken);

        var accessToken = response.AccessToken;
        if (response.IsError
            || !string.Equals(response.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(response.IssuedTokenType, OidcConstants.TokenTypeIdentifiers.AccessToken, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(accessToken)
            || string.Equals(accessToken, sourceAccessToken, StringComparison.Ordinal)
            || response.ExpiresIn <= 0)
        {
            throw new InvalidOperationException("The identity provider did not return a valid exchanged access token.");
        }

        DateTimeOffset expiresAt;
        try
        {
            expiresAt = _timeProvider.GetUtcNow().AddSeconds(response.ExpiresIn);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new InvalidOperationException("The identity provider returned an invalid exchanged token lifetime.");
        }

        await StoreAccessToken(sourceAccessToken, accessToken, expiresAt, cancellationToken);
        return accessToken;
    }

    private async Task<string?> GetCachedAccessToken(string sourceAccessToken, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(sourceAccessToken);
        var protectedEntry = await _cache.GetAsync(cacheKey, cancellationToken);
        if (protectedEntry is null)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(GetProtector(cacheKey).Unprotect(protectedEntry));
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var accessToken = reader.ReadString();
            if (stream.Position != stream.Length
                || string.IsNullOrWhiteSpace(accessToken)
                || string.Equals(accessToken, sourceAccessToken, StringComparison.Ordinal)
                || expiresAt <= _timeProvider.GetUtcNow().Add(RefreshBeforeExpiration))
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                return null;
            }

            return accessToken;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or EndOfStreamException or FormatException or InvalidDataException or OverflowException)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return null;
        }
    }

    private Task StoreAccessToken(string sourceAccessToken, string accessToken, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(expiresAt.ToUnixTimeMilliseconds());
            writer.Write(accessToken);
            writer.Flush();
        }

        return _cache.SetAsync(
            GetCacheKey(sourceAccessToken),
            GetProtector(GetCacheKey(sourceAccessToken)).Protect(stream.ToArray()),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            cancellationToken);
    }

    private static string GetSourceAccessToken(HttpRequestMessage request)
    {
        var authorization = request.Headers.Authorization;
        if (authorization is null
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            throw new InvalidOperationException("A managed management access token is required.");
        }

        return authorization.Parameter;
    }

    private string GetCacheKey(string sourceAccessToken)
    {
        var sourceTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(sourceAccessToken));
        return string.Concat(AudienceTokenCacheKeyPrefix, _audience, ':', WebEncoders.Base64UrlEncode(sourceTokenHash));
    }

    private IDataProtector GetProtector(string cacheKey)
    {
        return _protector.CreateProtector(cacheKey);
    }

    private static async Task<string> GetTokenEndpoint(OpenIdConnectOptions options, CancellationToken cancellationToken)
    {
        var tokenEndpoint = options.Configuration?.TokenEndpoint;
        if (string.IsNullOrWhiteSpace(tokenEndpoint) && options.ConfigurationManager is not null)
        {
            var configuration = await options.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            tokenEndpoint = configuration.TokenEndpoint;
        }

        if (!Uri.TryCreate(tokenEndpoint, UriKind.Absolute, out var endpoint)
            || (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !(!options.RequireHttpsMetadata && string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException("The identity provider token endpoint is unavailable.");
        }

        return endpoint.AbsoluteUri;
    }

    private static string GetSupportedAudience(string audience)
    {
        return audience switch
        {
            ApiAudienceNames.Admin => ApiAudienceNames.Admin,
            ApiAudienceNames.Catalog => ApiAudienceNames.Catalog,
            ApiAudienceNames.Branding => ApiAudienceNames.Branding,
            _ => throw new ArgumentOutOfRangeException(nameof(audience), "The backend audience is not configured for token exchange.")
        };
    }
}
