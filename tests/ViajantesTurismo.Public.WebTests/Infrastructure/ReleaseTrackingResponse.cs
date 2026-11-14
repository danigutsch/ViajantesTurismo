namespace ViajantesTurismo.Public.WebTests.Infrastructure;

/// <summary>An upstream response that records whether it was disposed.</summary>
internal sealed class ReleaseTrackingResponse : HttpResponseMessage
{
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets a task that completes when the response is disposed.</summary>
    public Task Released => released.Task;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            released.TrySetResult();
        }

        base.Dispose(disposing);
    }
}
