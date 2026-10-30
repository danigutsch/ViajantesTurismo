using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Authentication handler that signs every request in as a synthetic management user.</summary>
/// <param name="options">Scheme options monitor.</param>
/// <param name="logger">Logger factory.</param>
/// <param name="encoder">URL encoder.</param>
internal sealed class AlwaysAuthenticatedHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Name of the synthetic authentication scheme.</summary>
    internal const string SchemeName = "BrowserTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "browser-test")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
