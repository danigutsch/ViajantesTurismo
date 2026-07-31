namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class BlockingReadStream(byte[] prefix, byte[] remainder) : Stream
{
    private readonly TaskCompletionSource blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        await destination.WriteAsync(prefix, cancellationToken);
        _ = blocked.TrySetResult();
        await release.Task.WaitAsync(cancellationToken);
        await destination.WriteAsync(remainder, cancellationToken);
    }

    public Task WaitUntilBlocked(CancellationToken ct) => blocked.Task.WaitAsync(ct);

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
