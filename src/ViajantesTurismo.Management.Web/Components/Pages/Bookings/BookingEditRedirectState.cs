namespace ViajantesTurismo.Management.Web.Components.Pages.Bookings;

/// <summary>
/// Manages the transient redirect state used after a successful booking update.
/// </summary>
internal sealed class BookingEditRedirectState : IAsyncDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>Whether a redirect after a successful update is waiting to happen.</summary>
    internal bool IsPending
    {
        get; private set;
    }

    /// <summary>Whether the pending redirect was cancelled by the user.</summary>
    internal bool IsCancelled
    {
        get; private set;
    }

    /// <summary>Clears the pending and cancelled flags and releases the cancellation source.</summary>
    internal async Task Reset()
    {
        IsPending = false;
        IsCancelled = false;
        await DisposeCancellation();
    }

    /// <summary>Starts a pending redirect and returns the token that cancels it.</summary>
    internal CancellationToken BeginPendingRedirect()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        IsPending = true;
        IsCancelled = false;
        return _cancellationTokenSource.Token;
    }

    /// <summary>Cancels the pending redirect and marks it cancelled.</summary>
    internal async Task CancelPendingRedirect()
    {
        IsPending = false;
        IsCancelled = true;
        await DisposeCancellation();
    }

    /// <summary>Returns whether navigation may proceed: a redirect is pending and the token is not cancelled.</summary>
    internal bool CanNavigate(CancellationToken cancellationToken) =>
        IsPending && !cancellationToken.IsCancellationRequested;

    /// <summary>Cancels and disposes the current cancellation source, if any.</summary>
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

    /// <summary>Releases the cancellation source.</summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeCancellation();
    }
}
