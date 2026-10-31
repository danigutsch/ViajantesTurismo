namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>A read-only stream wrapper that records whether it was disposed.</summary>
public sealed class DisposalTrackingStream(Stream inner, bool stallAfterFirstRead = false) : Stream
{
    private bool hasRead;
    private readonly TaskCompletionSource stalledReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets a task that completes when the stalled read has begun waiting for cancellation.</summary>
    public Task StalledReadStarted => stalledReadStarted.Task;

    private readonly TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets a task that completes when the stream is disposed.</summary>
    public Task Disposed => disposed.Task;

    public bool IsDisposed
    {
        get; private set;
    }

    /// <summary>Gets a value indicating whether a stalled read observed cancellation.</summary>
    public bool StalledReadWasCancelled
    {
        get; private set;
    }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (stallAfterFirstRead && hasRead)
        {
            stalledReadStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                StalledReadWasCancelled = true;
                throw;
            }
        }

        hasRead = true;
        return await inner.ReadAsync(stallAfterFirstRead && buffer.Length > 1 ? buffer[..1] : buffer, cancellationToken).ConfigureAwait(false);
    }

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
            inner.Dispose();
            disposed.TrySetResult();
        }

        base.Dispose(disposing);
    }
}
