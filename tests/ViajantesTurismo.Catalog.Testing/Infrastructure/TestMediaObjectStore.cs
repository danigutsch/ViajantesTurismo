using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestMediaObjectStore : IMediaObjectStore
{
    private readonly Dictionary<string, MediaObjectWriteRequest> objects = [];

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);
        objects[request.ObjectKey] = request with { Content = new MemoryStream(content.ToArray()), Length = content.Length };

        return new MediaObjectWriteResult(request.ObjectKey, GetPublicUri(request.ObjectKey), GetPublicUri(request.ObjectKey), request.ContentType, content.Length, request.Checksum);
    }

    public async ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var request = objects[objectKey];
        request.Content.Position = 0;
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);

        return new MediaObjectReadResult(objectKey, new MemoryStream(content.ToArray()), request.ContentType, request.Length, request.Checksum);
    }

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(objects.ContainsKey(objectKey));
    }

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<string>>([.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)]);
    }

    public ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>([.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal).Select(key => new MediaObjectInventoryItem(key, DateTimeOffset.UtcNow))]);
    }

    public Uri GetPublicUri(string objectKey) => new($"https://cdn.example/{objectKey}");

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        objects.Remove(objectKey);
        return ValueTask.CompletedTask;
    }
}
