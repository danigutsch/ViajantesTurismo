namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Holds overview messaging reads while Chromium observes the refresh state.</summary>
internal sealed class OperationsOverviewRefreshGate(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly TaskCompletionSource _refreshAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _hold;
    private int _refreshRequests;

    /// <summary>Gets the number of overview reads made after the gate was armed.</summary>
    public int RefreshRequests => Volatile.Read(ref _refreshRequests);

    /// <summary>Holds subsequent overview reads until the test releases them.</summary>
    public void HoldRefresh() => Volatile.Write(ref _hold, 1);

    /// <summary>Releases the overview response.</summary>
    public void AllowRefresh() => _refreshAllowed.TrySetResult();

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (Volatile.Read(ref _hold) != 0 && request.RequestUri?.AbsolutePath == "/api/v1/operations/messaging")
        {
            Interlocked.Increment(ref _refreshRequests);
            await _refreshAllowed.Task.WaitAsync(ct);
        }

        return respond(request);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AllowRefresh();
        }

        base.Dispose(disposing);
    }
}
