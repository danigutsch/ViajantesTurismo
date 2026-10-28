using PandaBear.ObjectStorage;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Object store fake that drains written content without retaining it and records what each write looked like.</summary>
internal sealed class DiscardingMediaObjectStore : IObjectStore
{
    /// <summary>Gets the position of the content when the last write began.</summary>
    public long? LastWriteStartPosition
    {
        get; private set;
    }

    /// <summary>Gets the number of bytes drained from the content by the last write.</summary>
    public long LastWriteBytes
    {
        get; private set;
    }

    /// <summary>Gets the declared length of the last write request.</summary>
    public long LastWriteDeclaredLength
    {
        get; private set;
    }

    /// <summary>Drains the request content into <see cref="Stream.Null" /> and records its start position and size.</summary>
    public async ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        LastWriteStartPosition = request.Content.Position;
        LastWriteDeclaredLength = request.Length;
        await request.Content.CopyToAsync(Stream.Null, ct);
        LastWriteBytes = request.Content.Position - LastWriteStartPosition.Value;

        return new ObjectWriteResult(
            request.ObjectKey,
            new Uri($"file:///media/{request.ObjectKey}"),
            new Uri($"https://cdn.example/{request.ObjectKey}"),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    /// <summary>Not supported by this fake.</summary>
    public ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Not supported by this fake.</summary>
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Not supported by this fake.</summary>
    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Not supported by this fake.</summary>
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Not supported by this fake.</summary>
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

    /// <summary>Returns a deterministic CDN-style public URI for the key.</summary>
    public Uri GetPublicUri(string objectKey) => new($"https://cdn.example/{objectKey}");

    /// <summary>Accepts the delete without effect.</summary>
    public ValueTask Delete(string objectKey, CancellationToken ct) => ValueTask.CompletedTask;
}
