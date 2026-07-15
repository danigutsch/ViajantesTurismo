using System.Net.Http.Headers;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class SourceAccessTokenHandler(string accessToken, string scheme = "Bearer") : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, accessToken);
        return base.SendAsync(request, cancellationToken);
    }
}
