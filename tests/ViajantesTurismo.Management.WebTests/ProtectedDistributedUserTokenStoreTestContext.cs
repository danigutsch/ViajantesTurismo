using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Wires protected distributed token stores over an in-memory cache for tests.</summary>
internal sealed class ProtectedDistributedUserTokenStoreTestContext
{
    /// <summary>Creates the context, defaulting to an in-memory cache.</summary>
    public ProtectedDistributedUserTokenStoreTestContext(IDistributedCache? cache = null)
    {
        Cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        AudienceTokenStore = new ProtectedDistributedAudienceTokenStore(Cache, dataProtectionProvider, TimeProvider.System);
        Store = ProtectedDistributedUserTokenStore.CreateForTesting(Cache, dataProtectionProvider, TimeProvider.System);
    }

    /// <summary>Cache backing the stores.</summary>
    public IDistributedCache Cache
    {
        get;
    }

    /// <summary>Audience token store under test.</summary>
    public ProtectedDistributedAudienceTokenStore AudienceTokenStore
    {
        get;
    }

    /// <summary>User token store under test.</summary>
    public ProtectedDistributedUserTokenStore Store
    {
        get;
    }

    /// <summary>Creates a principal carrying the session id and session expiry claims.</summary>
    public static ClaimsPrincipal CreateUser(string sessionId, DateTimeOffset? sessionExpiresAt = null)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, sessionId),
            new Claim(
                ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType,
                (sessionExpiresAt ?? TimeProvider.System.GetUtcNow().Add(ManagementAuthenticationDefaults.SessionLifetime)).ToString("O"))
        ],
        "test");

        return new ClaimsPrincipal(identity);
    }

    /// <summary>Creates a bearer user token with the given access token expiring in five minutes.</summary>
    public static UserToken CreateToken(string accessToken)
    {
        return new UserToken
        {
            AccessToken = AccessToken.Parse(accessToken),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            ClientId = ClientId.Parse("web-app"),
            Expiration = TimeProvider.System.GetUtcNow().AddMinutes(5),
            RefreshToken = RefreshToken.Parse("refresh-token")
        };
    }

    /// <summary>Returns the cache key used for the given session id.</summary>
    public static string GetCacheKey(string sessionId)
    {
        return string.Concat(ManagementAuthenticationDefaults.UserTokenStoreKeyPrefix, sessionId);
    }
}
