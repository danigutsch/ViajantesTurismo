using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Test authentication handler that signs in a test user and records challenge and sign-out in response headers.</summary>
/// <param name="options">Scheme options monitor.</param>
/// <param name="logger">Logger factory.</param>
/// <param name="encoder">URL encoder.</param>
internal sealed class RecordingAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
{
    /// <summary>Response header carrying the challenge redirect URI.</summary>
    internal const string ChallengeRedirectHeaderName = "X-Test-Challenge-Redirect-Uri";
    /// <summary>Response header carrying the scheme name that was signed out.</summary>
    internal const string SignOutSchemeHeaderName = "X-Test-Sign-Out-Scheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Admin";
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "test-user"), new Claim(ClaimTypes.Role, role)],
            Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append(ChallengeRedirectHeaderName, properties.RedirectUri ?? string.Empty);
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <summary>Records the signed-out scheme in a response header, redirecting for the OpenID Connect scheme.</summary>
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
