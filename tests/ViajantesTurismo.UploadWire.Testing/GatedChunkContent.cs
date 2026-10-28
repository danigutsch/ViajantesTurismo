using System.Net;

namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>
/// Request content that sends a fixed payload in chunks, holding each chunk back until the test releases it.
/// </summary>
public sealed class GatedChunkContent : HttpContent
{
    private readonly ReadOnlyMemory<byte> payload;
    private readonly int chunkSize;
    private readonly SemaphoreSlim gate = new(0);
    private int chunksStarted;

    /// <summary>Creates content for the payload, split into chunks of at most <paramref name="chunkSize"/> bytes.</summary>
    public GatedChunkContent(ReadOnlyMemory<byte> payload, int chunkSize, string contentType)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);

        this.payload = payload;
        this.chunkSize = chunkSize;
        Headers.TryAddWithoutValidation("Content-Type", contentType);
    }

    /// <summary>Gets the number of chunks the sender has begun to write.</summary>
    public int ChunksStarted => Volatile.Read(ref chunksStarted);

    /// <summary>Gets the number of chunks the payload is split into.</summary>
    public int ChunkCount => (payload.Length + chunkSize - 1) / chunkSize;

    /// <summary>Gets the number of payload bytes sent once <paramref name="chunks"/> chunks are written.</summary>
    public long BytesAfter(int chunks) => Math.Min(payload.Length, (long)chunks * chunkSize);

    /// <summary>Lets the sender write exactly one more chunk.</summary>
    public void ReleaseChunk() => gate.Release();

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        await SerializeToStreamAsync(stream, context, CancellationToken.None).ConfigureAwait(false);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        for (var offset = 0; offset < payload.Length; offset += chunkSize)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            _ = Interlocked.Increment(ref chunksStarted);
            await stream.WriteAsync(payload.Slice(offset, Math.Min(chunkSize, payload.Length - offset)), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = payload.Length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gate.Dispose();
        }

        base.Dispose(disposing);
    }
}
