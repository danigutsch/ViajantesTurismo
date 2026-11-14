namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>A primary handler that answers every request with one status code and counts the requests it receives.</summary>
internal sealed class CountingStatusHandler(HttpStatusCode status) : HttpMessageHandler
{
    private int _requests;

    /// <summary>The number of requests that reached this handler.</summary>
    public int Requests => Volatile.Read(ref _requests);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requests);
        return Task.FromResult(new HttpResponseMessage(status) { RequestMessage = request });
    }
}
