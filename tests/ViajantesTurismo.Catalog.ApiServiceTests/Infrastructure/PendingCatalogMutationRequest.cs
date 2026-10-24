namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class PendingCatalogMutationRequest(HttpClient client, HttpRequestMessage request, IAsyncDisposable heldLease) : IAsyncDisposable
{
    private Task<HttpResponseMessage>? pending;

    public bool IsCompleted => pending?.IsCompleted == true;

    public void Start(CancellationToken ct)
    {
        pending = client.SendAsync(request, ct);
    }

    public async Task<HttpResponseMessage> Complete()
    {
        await heldLease.DisposeAsync();
        return await (pending ?? throw new InvalidOperationException("The request has not started."));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (pending is not null)
            {
                using var response = await Complete();
            }
            else
            {
                await heldLease.DisposeAsync();
            }
        }
        finally
        {
            request.Dispose();
        }
    }
}
