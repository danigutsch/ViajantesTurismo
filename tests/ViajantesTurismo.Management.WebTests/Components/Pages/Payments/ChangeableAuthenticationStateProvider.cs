using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Authentication state provider whose signed-in user a test can replace, raising the change event.</summary>
/// <remarks>Creates a provider that starts with the given user.</remarks>
internal sealed class ChangeableAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    private Task<AuthenticationState> _state = Task.FromResult(new AuthenticationState(user));

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state;

    /// <summary>Replaces the current user and notifies subscribers the way a real provider does.</summary>
    public void Change(ClaimsPrincipal user)
    {
        _state = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(_state);
    }
}
