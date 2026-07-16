using ViajantesTurismo.Catalog.Application.Media;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class InMemoryMediaObjectStore : IMediaObjectStore
{
    private readonly Dictionary<string, StoredMediaObject> objects = [];
    private readonly HashSet<string> failingDeletes = [];
    private Exception? openReadException;

    private const int MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength;

    public IReadOnlyCollection<string> ObjectKeys => objects.Keys;

    public int ExistsCallCount { get; private set; }

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(request.ObjectKey);
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct);
        objects[request.ObjectKey] = new StoredMediaObject(
            request with { Content = new MemoryStream(content.ToArray()), Length = content.Length },
            DateTimeOffset.UtcNow);

        return new MediaObjectWriteResult(
            request.ObjectKey,
            new Uri($"file:///media/{request.ObjectKey}"),
            GetPublicUri(request.ObjectKey),
            request.ContentType,
            content.Length,
            request.Checksum);
    }

    public async ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
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

        return new MediaObjectReadResult(
            objectKey,
            new MemoryStream(source.ToArray()),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        ExistsCallCount++;

        return ValueTask.FromResult(objects.ContainsKey(objectKey));
    }

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);

        return ValueTask.FromResult<IReadOnlyList<string>>(
            [.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)]);
    }

    public ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);

        return ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>(
            [.. objects.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(item => new MediaObjectInventoryItem(item.Key, item.Value.LastModifiedAt))
                .OrderBy(item => item.ObjectKey, StringComparer.Ordinal)]);
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public Uri GetPublicUri(string objectKey)
    {
        ValidateObjectKey(objectKey);
        return new Uri($"https://cdn.example/{objectKey}");
    }

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

    public void SetLastModified(string objectKey, DateTimeOffset lastModifiedAt)
    {
        ValidateObjectKey(objectKey);
        objects[objectKey] = objects[objectKey] with { LastModifiedAt = lastModifiedAt };
    }

    public void FailNextDelete(string objectKey)
    {
        ValidateObjectKey(objectKey);
        failingDeletes.Add(objectKey);
    }

    public void FailNextOpen(Exception exception)
    {
        openReadException = exception;
    }

    private sealed record StoredMediaObject(MediaObjectWriteRequest Request, DateTimeOffset LastModifiedAt);

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
