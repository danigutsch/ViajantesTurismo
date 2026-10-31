namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>
/// A non-seekable read-only stream over generated or given bytes that returns at most a fixed number of bytes per read,
/// can cancel a token mid-read, and records disposal and the bytes handed out.
/// </summary>
/// <param name="bytes">The bytes to serve.</param>
/// <param name="chunkSize">The most bytes returned by one read.</param>
/// <param name="cancelAfterFirstRead">A source cancelled during the first read, or null.</param>
internal sealed class OneChunkReadStream(byte[] bytes, int chunkSize = 4096, CancellationTokenSource? cancelAfterFirstRead = null) : Stream
{
    private int _position;

    /// <summary>Gets the number of read calls made.</summary>
    public int ReadCalls
    {
        get; private set;
    }

    /// <summary>Gets a value indicating whether the stream was disposed.</summary>
    public bool IsDisposed
    {
        get; private set;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCalls++;
        var count = Math.Min(Math.Min(buffer.Length, chunkSize), bytes.Length - _position);
        bytes.AsMemory(_position, count).CopyTo(buffer);
        _position += count;

        if (cancelAfterFirstRead is not null && ReadCalls == 1)
        {
            await cancelAfterFirstRead.CancelAsync().ConfigureAwait(false);
        }

        return count;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
