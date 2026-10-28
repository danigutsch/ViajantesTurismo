namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>Records every scan: the exact bytes the scanner received and how many client chunks had started by then.</summary>
public sealed class ScanRecorder
{
    private readonly Lock gate = new();
    private readonly List<RecordedScan> scans = [];
    private readonly TaskCompletionSource invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets the source of the "client chunks started" figure captured with each scan.</summary>
    public Func<int> ChunksStarted
    {
        get; set;
    } = () => 0;

    /// <summary>Completes when the first scan is requested.</summary>
    public Task Invoked => invoked.Task;

    /// <summary>Gets the scans recorded so far.</summary>
    public IReadOnlyList<RecordedScan> Scans
    {
        get
        {
            lock (gate)
            {
                return [.. scans];
            }
        }
    }

    /// <summary>Reads the content to the end and records it.</summary>
    public async ValueTask Record(Stream content, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var chunksStarted = ChunksStarted();
        using var captured = new MemoryStream();
        await content.CopyToAsync(captured, ct).ConfigureAwait(false);
        lock (gate)
        {
            scans.Add(new RecordedScan(captured.ToArray(), chunksStarted));
        }

        invoked.TrySetResult();
    }
}
