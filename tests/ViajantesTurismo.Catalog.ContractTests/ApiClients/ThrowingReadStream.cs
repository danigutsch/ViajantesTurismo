namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

/// <summary>Non-seekable, read-only stream whose every read throws the supplied exception, for testing client behavior on request-body read failures.</summary>
/// <param name="exception">The exception thrown by every read.</param>
internal sealed class ThrowingReadStream(IOException exception) : Stream
{
    /// <summary>Number of read attempts made so far.</summary>
    public int ReadCount
    {
        get; private set;
    }

    /// <summary>Whether the stream has been disposed.</summary>
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

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
    {
        ReadCount++;
        return ValueTask.FromException<int>(exception);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>Test handler that forces the request content to be read, so the failing stream throws, and otherwise returns 201 Created.</summary>
    public static HttpResponseMessage SerializeRequest(HttpRequestMessage request, CancellationToken ct)
    {
        request.Content.ShouldNotBeNull().ReadAsByteArrayAsync(ct).GetAwaiter().GetResult();
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
