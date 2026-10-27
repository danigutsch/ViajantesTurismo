namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

/// <summary>Polling helper that waits for an asynchronous condition in system tests.</summary>
internal static class Eventually
{
    /// <summary>Polls the probe every 250 ms until it returns a non-null result, throwing <see cref="TimeoutException" /> after the timeout.</summary>
    public static async Task<T> Until<T>(Func<CancellationToken, Task<T?>> probe, TimeSpan timeout, CancellationToken ct)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(probe);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        ct.ThrowIfCancellationRequested();

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                var result = await probe(linkedCts.Token);
                if (result is not null)
                {
                    return result;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(250), linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"Expected condition was not met within {timeout}.");
        }

        ct.ThrowIfCancellationRequested();

        throw new TimeoutException($"Expected condition was not met within {timeout}.");
    }
}
