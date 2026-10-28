namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>
/// Server-side observation of one upload request: when it starts, when its request token fires, how many body
/// bytes the pipeline reads (and in what sizes), and when it finishes.
/// </summary>
/// <param name="pathSuffix">Only requests whose path ends with this suffix are observed.</param>
public sealed class RequestLifetimeProbe(string pathSuffix)
{
    private readonly Lock gate = new();
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource aborted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<(long Target, TaskCompletionSource Reached)> waiters = [];
    private long bytesRead;

    /// <summary>Gets the path suffix of the observed request.</summary>
    public string PathSuffix => pathSuffix;

    /// <summary>Completes when the observed request enters the pipeline.</summary>
    public Task Started => started.Task;

    /// <summary>Completes when the request cancellation token of the observed request fires.</summary>
    public Task Aborted => aborted.Task;

    /// <summary>Completes when the pipeline has returned for the observed request.</summary>
    public Task Finished => finished.Task;

    /// <summary>Gets the response status code set when the pipeline returned.</summary>
    public int StatusCode
    {
        get; private set;
    }

    /// <summary>Gets the largest buffer size any body read requested.</summary>
    public int LargestRequestedRead
    {
        get; private set;
    }

    /// <summary>Gets the number of body bytes returned to readers so far.</summary>
    public long BytesRead
    {
        get
        {
            lock (gate)
            {
                return bytesRead;
            }
        }
    }

    /// <summary>
    /// Completes once at least <paramref name="count"/> body bytes were returned to readers, and fails fast with the
    /// response status when the request finishes first.
    /// </summary>
    public async Task WaitForBytes(long count)
    {
        Task reached;
        lock (gate)
        {
            if (bytesRead >= count)
            {
                return;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((count, waiter));
            reached = waiter.Task;
        }

        if (await Task.WhenAny(reached, finished.Task).ConfigureAwait(false) != reached)
        {
            throw new InvalidOperationException($"The request finished with status {StatusCode} after {BytesRead} body bytes, before {count} bytes were read.");
        }
    }

    /// <summary>
    /// Gets whether the pipeline observed the client disappearing mid-body: either the request token fired, or the
    /// framework answered the incomplete body with <c>400</c> before the token callback ran.
    /// </summary>
    public bool DisconnectObserved => aborted.Task.IsCompleted || StatusCode == 400;

    /// <summary>Records the started event.</summary>
    internal void OnStarted() => started.TrySetResult();

    /// <summary>Records the aborted event.</summary>
    internal void OnAborted() => aborted.TrySetResult();

    /// <summary>Records that the pipeline returned with the given status code.</summary>
    internal void OnFinished(int statusCode)
    {
        StatusCode = statusCode;
        finished.TrySetResult();
    }

    /// <summary>Records a body read of the requested size that returned the given number of bytes.</summary>
    internal void OnRead(int requested, int returned)
    {
        List<TaskCompletionSource> ready = [];
        lock (gate)
        {
            LargestRequestedRead = Math.Max(LargestRequestedRead, requested);
            bytesRead += returned;
            ready.AddRange(waiters.Where(waiter => bytesRead >= waiter.Target).Select(waiter => waiter.Reached));
            waiters.RemoveAll(waiter => bytesRead >= waiter.Target);
        }

        foreach (var reached in ready)
        {
            reached.TrySetResult();
        }
    }
}
