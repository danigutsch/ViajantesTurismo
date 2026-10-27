using PandaBear.ObjectStorage;
using PandaBear.InputNormalization;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>In-memory object store fake that validates keys, records calls and can inject delete and open failures.</summary>
internal sealed class InMemoryMediaObjectStore : IObjectStore
{
    private readonly Dictionary<string, StoredMediaObject> objects = [];
    private readonly HashSet<string> failingDeletes = [];
    private Exception? openReadException;

    private const int MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength;

    /// <summary>Gets the keys of all objects currently stored.</summary>
    public IReadOnlyCollection<string> ObjectKeys => objects.Keys;

    /// <summary>Gets how many times <c>Exists</c> has been called.</summary>
    public int ExistsCallCount
    {
        get; private set;
    }

    /// <summary>Buffers the request content and stores it under its object key with the current UTC time.</summary>
    public async ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(request.ObjectKey);
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct);
        objects[request.ObjectKey] = new StoredMediaObject(
            request with
            {
                Content = new MemoryStream(content.ToArray()),
                Length = content.Length
            },
            TimeProvider.System.GetUtcNow());

        return new ObjectWriteResult(
            request.ObjectKey,
            new Uri($"file:///media/{request.ObjectKey}"),
            GetPublicUri(request.ObjectKey),
            request.ContentType,
            content.Length,
            request.Checksum);
    }

    /// <summary>Returns a copy of the stored content, throwing the injected exception once if one was queued.</summary>
    public async ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);

        if (openReadException is not null)
        {
            var exception = openReadException;
            openReadException = null;
            throw exception;
        }

        var request = objects[objectKey].Request;
        request.Content.Position = 0;
        using var source = new MemoryStream();
        await request.Content.CopyToAsync(source, ct);

        return new ObjectReadResult(
            objectKey,
            new MemoryStream(source.ToArray()),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    /// <summary>Reports whether an object exists for the key and counts the call.</summary>
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        ExistsCallCount++;

        return ValueTask.FromResult(objects.ContainsKey(objectKey));
    }

    /// <summary>Lists stored keys starting with the prefix in ordinal order.</summary>
    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);

        return ValueTask.FromResult<IReadOnlyList<string>>(
            [.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)]);
    }

    /// <summary>Lists stored objects with their last-modified times for the prefix, ordered by key.</summary>
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);

        return ValueTask.FromResult<IReadOnlyList<ObjectInventoryItem>>(
            [.. objects.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(item => new ObjectInventoryItem(item.Key, item.Value.LastModifiedAt))
                .OrderBy(item => item.ObjectKey, StringComparer.Ordinal)]);
    }

    /// <summary>Not supported by this fake; always throws <see cref="NotSupportedException" />.</summary>
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <summary>Returns a deterministic CDN-style public URI for the key.</summary>
    public Uri GetPublicUri(string objectKey)
    {
        ValidateObjectKey(objectKey);
        return new Uri($"https://cdn.example/{objectKey}");
    }

    /// <summary>Removes the object, or throws <see cref="IOException" /> once if the key was marked with <c>FailNextDelete</c>.</summary>
    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        if (failingDeletes.Remove(objectKey))
        {
            throw new IOException($"Delete failed for {objectKey}.");
        }

        objects.Remove(objectKey);

        return ValueTask.CompletedTask;
    }

    /// <summary>Overrides the last-modified time of a stored object, for orphan age scenarios.</summary>
    public void SetLastModified(string objectKey, DateTimeOffset lastModifiedAt)
    {
        ValidateObjectKey(objectKey);
        objects[objectKey] = objects[objectKey] with
        {
            LastModifiedAt = lastModifiedAt
        };
    }

    /// <summary>Makes the next delete of the key throw <see cref="IOException" />.</summary>
    public void FailNextDelete(string objectKey)
    {
        ValidateObjectKey(objectKey);
        failingDeletes.Add(objectKey);
    }

    /// <summary>Makes the next <c>OpenRead</c> call throw the given exception.</summary>
    public void FailNextOpen(Exception exception)
    {
        openReadException = exception;
    }

    private sealed record StoredMediaObject(ObjectWriteRequest Request, DateTimeOffset LastModifiedAt);

    private static void ValidateObjectKey(string objectKey)
    {
        if (!ObjectStorageKeyValidator.IsValidRelativeKey(objectKey, MaxObjectKeyLength))
        {
            throw new ArgumentException("Media object key must be a relative slash-delimited path without dot segments.", nameof(objectKey));
        }
    }

    private static void ValidatePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        var normalizedPrefix = prefix.TrimEnd('/');
        ValidateObjectKey(normalizedPrefix);
    }
}
