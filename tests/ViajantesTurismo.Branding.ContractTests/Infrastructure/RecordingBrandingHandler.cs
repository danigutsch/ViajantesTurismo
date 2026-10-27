using System.Net;

namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>
/// HTTP handler that records every request and answers with a canned response.
/// </summary>
/// <param name="respond">Builds the response for each request.</param>
internal sealed class RecordingBrandingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    /// <summary>Gets the requests received, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }

    /// <summary>Creates a JSON response with an optional entity tag.</summary>
    public static HttpResponseMessage Json(HttpStatusCode status, string body, string? entityTag = null)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        if (entityTag is not null)
        {
            response.Headers.ETag = System.Net.Http.Headers.EntityTagHeaderValue.Parse(entityTag);
        }

        return response;
    }
}
