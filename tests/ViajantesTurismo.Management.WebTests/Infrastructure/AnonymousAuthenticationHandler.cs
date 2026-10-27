using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Authentication handler that never authenticates anyone.</summary>
/// <param name="options">Scheme options monitor.</param>
/// <param name="logger">Logger factory.</param>
/// <param name="encoder">URL encoder.</param>
internal sealed class AnonymousAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Name of the test authentication scheme.</summary>
    internal const string SchemeName = "test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
