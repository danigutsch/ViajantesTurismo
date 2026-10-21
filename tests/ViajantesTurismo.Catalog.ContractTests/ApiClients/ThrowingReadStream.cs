namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

internal sealed class ThrowingReadStream(IOException exception) : Stream
{
    public int ReadCount
    {
        get; private set;
    }

    public bool IsDisposed
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCount++;
        throw exception;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return ValueTask.FromException<int>(exception);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public static HttpResponseMessage SerializeRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Content.ShouldNotBeNull().ReadAsByteArrayAsync(cancellationToken).GetAwaiter().GetResult();
        return new HttpResponseMessage(System.Net.HttpStatusCode.Created);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
        }

        base.Dispose(disposing);
    }
}
