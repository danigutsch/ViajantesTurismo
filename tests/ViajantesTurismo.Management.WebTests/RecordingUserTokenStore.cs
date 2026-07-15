using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class RecordingUserTokenStore : IUserTokenStore
{
    public ClaimsPrincipal? StoredUser { get; private set; }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<TokenResult<TokenForParameters>> GetTokenAsync(
        ClaimsPrincipal user,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        TokenResult<TokenForParameters> result = TokenResult.Failure("No token is recorded.");
        return Task.FromResult(result);
    }

    public Task StoreTokenAsync(
        ClaimsPrincipal user,
        UserToken token,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        StoredUser = user;
        return Task.CompletedTask;
    }
}
