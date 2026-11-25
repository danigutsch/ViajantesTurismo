namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Holds the next matching projection response after the transport has captured its snapshot.</summary>
internal sealed class PaymentProjectionResponseGate(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private (string PathSuffix, string QueryFragment)? _next;

    /// <summary>Selects a single financial read to pause on its way back to the component.</summary>
    internal void HoldNext(string pathSuffix, string queryFragment) => _next = (pathSuffix, queryFragment);

    /// <summary>Waits for the held response, completes the state change, then releases the original response.</summary>
    internal async Task CompleteAfter(Func<Task> start, Action update, CancellationToken ct)
    {
        var pending = start();
        try
        {
            await _started.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            update();
        }
        finally
        {
            _release.TrySetResult();
        }
        await pending;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);
        if (_next is not { } match || request.RequestUri is not { } uri ||
            !uri.AbsolutePath.EndsWith(match.PathSuffix, StringComparison.Ordinal) ||
            !uri.Query.Contains(match.QueryFragment, StringComparison.Ordinal))
        {
            return response;
        }

        _next = null;
        _started.TrySetResult();
        try
        {
            await _release.Task.WaitAsync(ct);
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
