using System.Net;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Backend handler that records the Authorization header of each request.</summary>
internal sealed class RecordingAudienceTokenBackendHandler : HttpMessageHandler
{
    /// <summary>Authorization headers received, null when absent.</summary>
    public List<string?> AuthorizationHeaders { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
