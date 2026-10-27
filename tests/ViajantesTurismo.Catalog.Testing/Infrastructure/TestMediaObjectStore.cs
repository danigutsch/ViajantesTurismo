using PandaBear.ObjectStorage;
using PandaBear.InputNormalization;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestMediaObjectStore : IObjectStore
{
    private readonly Dictionary<string, ObjectWriteRequest> objects = [];

    private const int MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength;

    private readonly List<DisposalTrackingStream> openedStreams = [];

    public bool ThrowFileNotFoundOnOpenRead
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether opened streams deliver one byte and then stall until cancelled.</summary>
    public bool StallReadsAfterFirstByte
    {
        get; set;
    }

    /// <summary>Gets the streams handed out by <see cref="OpenRead"/>, in call order.</summary>
    public IReadOnlyList<DisposalTrackingStream> OpenedStreams => openedStreams;

    public async ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(request.ObjectKey);
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);
        objects[request.ObjectKey] = request with
        {
            Content = new MemoryStream(content.ToArray()),
            Length = content.Length
        };

        return new ObjectWriteResult(request.ObjectKey, GetPublicUri(request.ObjectKey), GetPublicUri(request.ObjectKey), request.ContentType, content.Length, request.Checksum);
    }

    public async ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateObjectKey(objectKey);
        if (ThrowFileNotFoundOnOpenRead)
        {
            throw new FileNotFoundException("The media object is unavailable.", objectKey);
        }

        var request = objects[objectKey];
        request.Content.Position = 0;
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct).ConfigureAwait(false);

        var stream = new DisposalTrackingStream(new MemoryStream(content.ToArray()), StallReadsAfterFirstByte);
        openedStreams.Add(stream);
        return new ObjectReadResult(objectKey, stream, request.ContentType, request.Length, request.Checksum);
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

    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePrefix(prefix);
        return ValueTask.FromResult<IReadOnlyList<ObjectInventoryItem>>([.. objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal).Select(key => new ObjectInventoryItem(key, TimeProvider.System.GetUtcNow()))]);
    }

    public Uri GetPublicUri(string objectKey)
    {
        ValidateObjectKey(objectKey);
        return new Uri($"https://cdn.example/{objectKey}");
    }

    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

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
