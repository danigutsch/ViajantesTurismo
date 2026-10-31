namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Non-seekable stream that serves a prefix (through <c>CopyToAsync</c> or <c>ReadAsync</c>) and then throws.</summary>
/// <param name="prefix">Bytes written before the failure.</param>
/// <param name="exception">Exception thrown after the prefix is written.</param>
internal sealed class PrefixThenThrowStream(byte[] prefix, IOException exception) : Stream
{
    private int served;

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
        throw exception;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (served < prefix.Length)
        {
            var count = Math.Min(buffer.Length, prefix.Length - served);
            prefix.AsMemory(served, count).CopyTo(buffer);
            served += count;

            return ValueTask.FromResult(count);
        }

        throw exception;
    }

    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
