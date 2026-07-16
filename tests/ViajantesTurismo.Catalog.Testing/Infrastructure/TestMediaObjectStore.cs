using ViajantesTurismo.Catalog.Application.Media;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestMediaObjectStore : IMediaObjectStore
{
    private readonly Dictionary<string, MediaObjectWriteRequest> objects = [];

    private const int MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength;

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(request.ObjectKey);
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);
        objects[request.ObjectKey] = request with { Content = new MemoryStream(content.ToArray()), Length = content.Length };

        return new MediaObjectWriteResult(request.ObjectKey, GetPublicUri(request.ObjectKey), GetPublicUri(request.ObjectKey), request.ContentType, content.Length, request.Checksum);
    }

    public async ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        var request = objects[objectKey];
        request.Content.Position = 0;
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);

        return new MediaObjectReadResult(objectKey, new MemoryStream(content.ToArray()), request.ContentType, request.Length, request.Checksum);
    }

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        return ValueTask.FromResult(objects.ContainsKey(objectKey));
    }

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);
        return ValueTask.FromResult<IReadOnlyList<string>>([.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)]);
    }

    public ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);
        return ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>([.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal).Select(key => new MediaObjectInventoryItem(key, DateTimeOffset.UtcNow))]);
    }

    public Uri GetPublicUri(string objectKey)
    {
        ValidateObjectKey(objectKey);
        return new Uri($"https://cdn.example/{objectKey}");
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        objects.Remove(objectKey);
        return ValueTask.CompletedTask;
    }

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
