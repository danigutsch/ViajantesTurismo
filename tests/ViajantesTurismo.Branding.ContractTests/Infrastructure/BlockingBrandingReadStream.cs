namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>
/// Non-seekable stream whose reads block until cancelled, for testing cancellation and disposal.
/// </summary>
/// <param name="onReadStarted">Optional callback invoked once when the first read starts.</param>
internal sealed class BlockingBrandingReadStream(Action? onReadStarted = null) : Stream
{
    private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Action? _onReadStarted = onReadStarted;

    /// <summary>Gets whether the stream has been disposed.</summary>
    public bool IsDisposed
    {
        get; private set;
    }

    /// <summary>Gets a task that completes when the first read starts.</summary>
    public Task ReadStarted => _readStarted.Task;

    public override bool CanRead => !IsDisposed;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
    {
        _readStarted.TrySetResult();
        Interlocked.Exchange(ref _onReadStarted, null)?.Invoke();
        await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
        }

        base.Dispose(disposing);
    }
}
