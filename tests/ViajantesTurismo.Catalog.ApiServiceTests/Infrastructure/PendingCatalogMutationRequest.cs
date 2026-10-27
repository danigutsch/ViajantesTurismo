namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>A mutation request sent while a lease is held, completed after the lease is released.</summary>
/// <param name="client">Client that sends the request.</param>
/// <param name="request">Request to send.</param>
/// <param name="heldLease">Lease released when the request is completed.</param>
internal sealed class PendingCatalogMutationRequest(HttpClient client, HttpRequestMessage request, IAsyncDisposable heldLease) : IAsyncDisposable
{
    private Task<HttpResponseMessage>? pending;

    /// <summary>Whether the request has started and finished.</summary>
    public bool IsCompleted => pending?.IsCompleted == true;

    /// <summary>Starts sending the request.</summary>
    public void Start(CancellationToken ct)
    {
        pending = client.SendAsync(request, ct);
    }

    /// <summary>Releases the lease and returns the response; throws when not started.</summary>
    public async Task<HttpResponseMessage> Complete()
    {
        await heldLease.DisposeAsync();
        return await (pending ?? throw new InvalidOperationException("The request has not started."));
    }

    /// <summary>Completes any pending request or releases the lease, then disposes the request.</summary>
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
