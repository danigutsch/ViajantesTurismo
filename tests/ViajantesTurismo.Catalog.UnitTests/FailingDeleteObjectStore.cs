using PandaBear.ObjectStorage;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Object store decorator whose <c>Delete</c> always throws <see cref="IOException" /> and counts the calls.</summary>
/// <param name="inner">Store that receives every other call.</param>
internal sealed class FailingDeleteObjectStore(IObjectStore inner) : IObjectStore
{
    /// <summary>Gets how many deletes were attempted.</summary>
    public int DeleteCallCount
    {
        get; private set;
    }

    /// <inheritdoc />
    public ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct) => inner.Put(request, ct);

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
    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        DeleteCallCount++;

        throw new IOException("delete failed");
    }
}
