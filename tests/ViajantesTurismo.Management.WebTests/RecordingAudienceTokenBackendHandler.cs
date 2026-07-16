using System.Net;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class RecordingAudienceTokenBackendHandler : HttpMessageHandler
{
    public List<string?> AuthorizationHeaders { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
