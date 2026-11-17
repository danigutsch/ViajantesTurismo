namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Owns a pending logo request and waits for it to end before disposing its content.</summary>
internal sealed class PendingBrandingLogoUpload : IAsyncDisposable
{
    private readonly HttpRequestMessage request;
    private readonly CancellationTokenSource cancellation;

    /// <summary>Starts a logo request with cancellation controlled by the owning test.</summary>
    public PendingBrandingLogoUpload(HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        this.request = request;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Response = client.SendAsync(request, cancellation.Token);
    }

    /// <summary>Gets the task that completes when the client receives a response.</summary>
    public Task<HttpResponseMessage> Response
    {
        get;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await cancellation.CancelAsync();
            using var response = await Response;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Cancellation ends a gated or stalled client before request content is disposed.
        }
        finally
        {
            request.Dispose();
            cancellation.Dispose();
        }
    }
}
