using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

internal sealed class RecordingAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
{
    internal const string ChallengeRedirectHeaderName = "X-Test-Challenge-Redirect-Uri";
    internal const string SignOutSchemeHeaderName = "X-Test-Sign-Out-Scheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append(ChallengeRedirectHeaderName, properties.RedirectUri ?? string.Empty);
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties? properties)
    {
        Response.Headers.Append(SignOutSchemeHeaderName, Scheme.Name);

        if (string.Equals(Scheme.Name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            Response.StatusCode = StatusCodes.Status302Found;
        }

        return Task.CompletedTask;
    }
}
