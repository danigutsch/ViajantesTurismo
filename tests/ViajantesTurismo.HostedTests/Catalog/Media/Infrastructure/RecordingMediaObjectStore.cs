using PandaBear.ObjectStorage;

namespace ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

/// <summary>Object store fake that counts writes and rejects reads and direct uploads.</summary>
internal sealed class RecordingMediaObjectStore : IObjectStore
{
    /// <summary>Gets the number of writes made.</summary>
    public int PutCount
    {
        get; private set;
    }

    /// <summary>Counts the write and returns a fixed stored result echoing the request metadata.</summary>
    public ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        PutCount++;
        return ValueTask.FromResult(new ObjectWriteResult(
            "stored",
            new Uri("https://cdn.invalid/stored"),
            new Uri("https://cdn.invalid/stored"),
            request.ContentType,
            request.Length,
            request.Checksum));
    }

    /// <summary>Always throws because reads are not expected.</summary>
    public ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => throw new InvalidOperationException("Storage reads are not expected.");

    /// <summary>Always reports that the object does not exist.</summary>
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => ValueTask.FromResult(false);

    /// <summary>Returns no keys.</summary>
    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct) => ValueTask.FromResult<IReadOnlyList<string>>([]);

    /// <summary>Returns no objects.</summary>
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct) => ValueTask.FromResult<IReadOnlyList<ObjectInventoryItem>>([]);

    /// <summary>Returns a fixed placeholder public URI.</summary>
    public Uri GetPublicUri(string objectKey) => new("https://cdn.invalid/stored");

    /// <summary>Always throws because direct uploads are not expected.</summary>
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct) => throw new InvalidOperationException("Direct uploads are not expected.");

    /// <summary>Completes without deleting anything.</summary>
    public ValueTask Delete(string objectKey, CancellationToken ct) => ValueTask.CompletedTask;
}
