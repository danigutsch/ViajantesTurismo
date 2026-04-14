namespace ViajantesTurismo.Admin.Web.Components.Pages.Bookings;

/// <summary>
/// Manages the transient redirect state used after a successful booking update.
/// </summary>
internal sealed class BookingEditRedirectState : IAsyncDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    internal bool IsPending { get; private set; }

    internal bool IsCancelled { get; private set; }

    internal async Task Reset()
    {
        IsPending = false;
        IsCancelled = false;
        await DisposeCancellation();
    }

    internal CancellationToken BeginPendingRedirect()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        IsPending = true;
        IsCancelled = false;
        return _cancellationTokenSource.Token;
    }

    internal async Task CancelPendingRedirect()
    {
        IsPending = false;
        IsCancelled = true;
        await DisposeCancellation();
    }

    internal bool CanNavigate(CancellationToken cancellationToken) =>
        IsPending && !cancellationToken.IsCancellationRequested;

    internal async Task DisposeCancellation()
    {
        var cancellationTokenSource = _cancellationTokenSource;
        if (cancellationTokenSource is null)
        {
            return;
        }

        _cancellationTokenSource = null;
        await cancellationTokenSource.CancelAsync();
        cancellationTokenSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCancellation();
    }
}
