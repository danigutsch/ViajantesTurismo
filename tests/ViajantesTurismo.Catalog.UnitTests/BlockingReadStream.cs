namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Non-seekable read stream whose copy writes a prefix, then blocks until released, then writes the remainder (through <c>CopyToAsync</c> or <c>ReadAsync</c>).</summary>
/// <param name="prefix">Bytes copied before blocking.</param>
/// <param name="remainder">Bytes copied after release.</param>
internal sealed class BlockingReadStream(byte[] prefix, byte[] remainder) : Stream
{
    private readonly TaskCompletionSource blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int served;
    private int servedRemainder;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken ct)
    {
        await destination.WriteAsync(prefix, ct);
        _ = blocked.TrySetResult();
        await release.Task.WaitAsync(ct);
        await destination.WriteAsync(remainder, ct);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (served < prefix.Length)
        {
            return Serve(prefix, ref served, buffer);
        }

        _ = blocked.TrySetResult();
        await release.Task.WaitAsync(ct);

        return Serve(remainder, ref servedRemainder, buffer);
    }

    private static int Serve(byte[] source, ref int served, Memory<byte> buffer)
    {
        var count = Math.Min(buffer.Length, source.Length - served);
        source.AsMemory(served, count).CopyTo(buffer);
        served += count;

        return count;
    }

    /// <summary>Completes once the prefix was copied and the copy is blocked.</summary>
    public Task WaitUntilBlocked(CancellationToken ct) => blocked.Task.WaitAsync(ct);

    /// <summary>Lets a blocked copy continue.</summary>
    public void Release() => release.TrySetResult();

    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Release();
        }

        base.Dispose(disposing);
    }
}
