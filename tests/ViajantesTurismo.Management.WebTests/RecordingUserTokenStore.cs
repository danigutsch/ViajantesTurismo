using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>User token store that records the stored user and never returns a token.</summary>
internal sealed class RecordingUserTokenStore : IUserTokenStore
{
    /// <summary>User passed to the last store call, null before any.</summary>
    public ClaimsPrincipal? StoredUser
    {
        get; private set;
    }

    /// <summary>Does nothing.</summary>
    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>Always returns a failure result.</summary>
    public Task<TokenResult<TokenForParameters>> GetTokenAsync(
        ClaimsPrincipal user,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        TokenResult<TokenForParameters> result = TokenResult.Failure("No token is recorded.");
        return Task.FromResult(result);
    }

    /// <summary>Records the user and discards the token.</summary>
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
