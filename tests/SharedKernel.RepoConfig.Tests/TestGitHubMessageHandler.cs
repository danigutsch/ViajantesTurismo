using System.Net;

namespace SharedKernel.RepoConfig.Tests;

internal sealed class TestGitHubMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = [];

    public List<CapturedRequest> Requests { get; } = [];

    public void EnqueueJson(HttpStatusCode statusCode, string json)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json)
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CaptureAndDequeueResponse(request, cancellationToken));
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) =>
        CaptureAndDequeueResponse(request, cancellationToken);

    private HttpResponseMessage CaptureAndDequeueResponse(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri?.ToString() ?? string.Empty,
            request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult()));

        return _responses.Count == 0
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : _responses.Dequeue();
    }

    internal sealed record CapturedRequest(HttpMethod Method, string Uri, string? Body);
}
