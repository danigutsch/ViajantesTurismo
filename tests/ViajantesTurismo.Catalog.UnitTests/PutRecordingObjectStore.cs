using PandaBear.ObjectStorage;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Object store decorator that records what each <c>Put</c> request's content stream looked like before delegating.</summary>
/// <param name="inner">Store that receives every call.</param>
internal sealed class PutRecordingObjectStore(IObjectStore inner) : IObjectStore
{
    private readonly List<RecordedPut> puts = [];

    /// <summary>Snapshot of each <c>Put</c> request in call order.</summary>
    public IReadOnlyList<RecordedPut> Puts => puts;

    /// <summary>What a <c>Put</c> request's content stream looked like when the store received it.</summary>
    /// <param name="ObjectKey">Requested object key.</param>
    /// <param name="DeclaredLength">Length declared on the request.</param>
    /// <param name="CanWrite">Whether the content stream accepted writes.</param>
    /// <param name="Position">Stream position when received.</param>
    /// <param name="Bytes">All bytes the stream served from its start.</param>
    /// <param name="StreamType">Runtime type of the content stream.</param>
    internal sealed record RecordedPut(string ObjectKey, long DeclaredLength, bool CanWrite, long Position, byte[] Bytes, Type StreamType);

    /// <inheritdoc />
    public async ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var position = request.Content.Position;
        using var copy = new MemoryStream();
        await request.Content.CopyToAsync(copy, ct);
        request.Content.Position = position;
        puts.Add(new RecordedPut(request.ObjectKey, request.Length, request.Content.CanWrite, position, copy.ToArray(), request.Content.GetType()));
        return await inner.Put(request, ct);
    }

    /// <inheritdoc />
    public ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => inner.OpenRead(objectKey, ct);

    /// <inheritdoc />
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => inner.Exists(objectKey, ct);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct) => inner.ListKeys(prefix, ct);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct) => inner.ListObjects(prefix, ct);

    /// <inheritdoc />
    public Uri GetPublicUri(string objectKey) => inner.GetPublicUri(objectKey);

    /// <inheritdoc />
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct) => inner.CreateUploadUrl(request, ct);

    /// <inheritdoc />
    public ValueTask Delete(string objectKey, CancellationToken ct) => inner.Delete(objectKey, ct);
}
