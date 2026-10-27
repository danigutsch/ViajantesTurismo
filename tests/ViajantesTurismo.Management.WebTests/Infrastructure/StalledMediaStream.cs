namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Yields one byte, then blocks further reads until cancelled; records disposal and cancellation.</summary>
internal sealed class StalledMediaStream : Stream
{
    private bool hasRead;

    /// <summary>True once the stream has been disposed.</summary>
    public bool IsDisposed
    {
        get; private set;
    }

    /// <summary>True when the stalled read was cancelled.</summary>
    public bool StalledReadWasCancelled
    {
        get; private set;
    }

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

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (hasRead)
        {
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
        buffer.Span[0] = (byte)'i';
        return 1;
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
