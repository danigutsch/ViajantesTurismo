namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>
/// Non-seekable read stream that serves its content in fixed-size chunks and blocks every read until the test releases it.
/// </summary>
/// <param name="content">Bytes to serve.</param>
/// <param name="chunkSize">Maximum bytes returned per read.</param>
internal sealed class GatedChunkReadStream(byte[] content, int chunkSize) : Stream
{
    private readonly SemaphoreSlim gate = new(0);
    private readonly SemaphoreSlim pending = new(0);
    private int offset;

    /// <summary>Gets whether the stream was disposed.</summary>
    public bool IsDisposed
    {
        get; private set;
    }

    /// <summary>Gets the total bytes served to the reader.</summary>
    public long BytesRead => offset;

    /// <summary>Gets the largest read request received, including reads that did not return data.</summary>
    public int LargestRequestedRead
    {
        get; private set;
    }

    /// <summary>Completes when the reader is blocked on its next read.</summary>
    public Task WaitForPendingRead(CancellationToken ct) => pending.WaitAsync(ct);

    /// <summary>Lets exactly one blocked read continue.</summary>
    public void ReleaseRead() => gate.Release();

    /// <summary>Lets every current and future read continue.</summary>
    public void ReleaseAll() => gate.Release(int.MaxValue / 2);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
    {
        LargestRequestedRead = Math.Max(LargestRequestedRead, buffer.Length);
        _ = pending.Release();
        await gate.WaitAsync(ct).ConfigureAwait(false);

        var count = Math.Min(Math.Min(chunkSize, buffer.Length), content.Length - offset);
        content.AsMemory(offset, count).CopyTo(buffer);
        offset += count;
        return count;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
            gate.Dispose();
            pending.Dispose();
        }

        base.Dispose(disposing);
    }
}
