using System.Net.Http.Headers;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Delegating handler that stamps every outgoing request with a fixed Authorization header.</summary>
/// <param name="accessToken">Token placed in the Authorization header.</param>
/// <param name="scheme">Authorization scheme; <c>Bearer</c> by default.</param>
internal sealed class SourceAccessTokenHandler(string accessToken, string scheme = "Bearer") : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, accessToken);
        return base.SendAsync(request, cancellationToken);
    }
}
