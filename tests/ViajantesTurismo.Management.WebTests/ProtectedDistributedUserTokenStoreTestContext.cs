using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ProtectedDistributedUserTokenStoreTestContext
{
    public ProtectedDistributedUserTokenStoreTestContext(IDistributedCache? cache = null)
    {
        Cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        Store = new ProtectedDistributedUserTokenStore(Cache, new EphemeralDataProtectionProvider(), TimeProvider.System);
    }

    public IDistributedCache Cache { get; }

    public ProtectedDistributedUserTokenStore Store { get; }

    public static ClaimsPrincipal CreateUser(string sessionId, DateTimeOffset? sessionExpiresAt = null)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, sessionId),
            new Claim(
                ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType,
                (sessionExpiresAt ?? DateTimeOffset.UtcNow.Add(ManagementAuthenticationDefaults.SessionLifetime)).ToString("O"))
        ],
        "test");

        return new ClaimsPrincipal(identity);
    }

    public static UserToken CreateToken(string accessToken)
    {
        return new UserToken
        {
            AccessToken = AccessToken.Parse(accessToken),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            ClientId = ClientId.Parse("web-app"),
            Expiration = DateTimeOffset.UtcNow.AddMinutes(5),
            RefreshToken = RefreshToken.Parse("refresh-token")
        };
    }

    public static string GetCacheKey(string sessionId)
    {
        return string.Concat(ManagementAuthenticationDefaults.UserTokenStoreKeyPrefix, sessionId);
    }
}
