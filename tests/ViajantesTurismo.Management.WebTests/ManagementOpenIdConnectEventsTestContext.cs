using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ViajantesTurismo.Management.WebTests;

internal static class ManagementOpenIdConnectEventsTestContext
{
    public static TokenValidatedContext Create(
        ClaimsPrincipal? principal = null,
        string? accessToken = "access-token",
        string? refreshToken = "refresh-token",
        string? tokenType = "Bearer",
        string? expiresIn = "300",
        string? clientId = "web-app")
    {
        return new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(OpenIdConnectHandler)),
            new OpenIdConnectOptions { ClientId = clientId },
            principal ?? new ClaimsPrincipal(new ClaimsIdentity("provider")),
            new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage(),
            TokenEndpointResponse = new OpenIdConnectMessage
            {
                AccessToken = accessToken,
                ExpiresIn = expiresIn,
                RefreshToken = refreshToken,
                TokenType = tokenType
            }
        };
    }

    public static TokenValidatedContext CreateWithoutTokenResponse()
    {
        var context = Create();
        context.TokenEndpointResponse = null;
        return context;
    }
}
