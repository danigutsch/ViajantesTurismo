namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Waits before an automatic redirect and reports whether the user let it run.
/// </summary>
internal static class RedirectDelay
{
    /// <summary>
    /// Waits for the delay to elapse.
    /// </summary>
    /// <param name="milliseconds">The time to wait before the redirect.</param>
    /// <param name="ct">The token the user cancels by choosing to stay on the page.</param>
    /// <returns><see langword="true"/> when the delay elapsed and was not canceled; <see langword="false"/> when the user canceled the redirect.</returns>
    public static async Task<bool> Elapsed(int milliseconds, CancellationToken ct)
    {
        try
        {
            await Task.Delay(milliseconds, ct);
            return !ct.IsCancellationRequested;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
