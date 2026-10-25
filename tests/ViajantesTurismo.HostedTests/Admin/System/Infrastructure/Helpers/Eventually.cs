namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal static class Eventually
{
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
