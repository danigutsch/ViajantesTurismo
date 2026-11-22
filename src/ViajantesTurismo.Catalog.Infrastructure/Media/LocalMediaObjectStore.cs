using Microsoft.Extensions.Options;
using PandaBear.ObjectStorage;
using PandaBear.ObjectStorage.FileSystem;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure.Media;

/// <summary>Adapts Catalog's key, media type, and public address policy to local storage.</summary>
internal sealed class LocalMediaObjectStore(IOptions<LocalMediaObjectStorageOptions> storageOptions) : IObjectStore
{
    private const string UriPathSeparator = "/";

    private readonly FileSystemObjectStore store = new(new FileSystemObjectStoreOptions(
        storageOptions.Value.RootPath,
        key => PublicAddress(storageOptions.Value.PublicBaseUri, key),
        key => (GetContentType(key.Value), null))
    {
        MaxKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength,
        KeyAllowed = key => !key.Value.StartsWith("..", StringComparison.Ordinal),
        TemporaryFilePrefix = ".viajantes-",
    });

    /// <inheritdoc />
    public ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct) => store.Put(request, ct);

    /// <inheritdoc />
    public ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => store.OpenRead(objectKey, ct);

    /// <inheritdoc />
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => store.Exists(objectKey, ct);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return store.ListKeys(NormalizePrefix(prefix), ct);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return store.ListObjects(NormalizePrefix(prefix), ct);
    }

    /// <inheritdoc />
    public Uri GetPublicUri(string objectKey) => store.GetPublicUri(objectKey);

    /// <inheritdoc />
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        throw new NotSupportedException("Local media storage does not support direct upload tickets.");
    }

    /// <inheritdoc />
    public ValueTask Delete(string objectKey, CancellationToken ct) => store.Delete(objectKey, ct);

    private static Uri PublicAddress(Uri baseAddress, ObjectStorageKey key)
    {
        var kind = baseAddress.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
        var baseUri = baseAddress.OriginalString.EndsWith(UriPathSeparator, StringComparison.Ordinal)
            ? baseAddress
            : new Uri(baseAddress.OriginalString + UriPathSeparator, kind);
        var escapedKey = key.ToEscapedPath();
        return baseUri.IsAbsoluteUri
            ? new Uri(baseUri, escapedKey)
            : new Uri(baseUri.OriginalString + escapedKey, UriKind.Relative);
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        if (!ObjectStoragePrefix.TryFrom(prefix, out var parsedPrefix, CatalogDomainLimits.MaxMediaObjectKeyLength))
        {
            throw new ArgumentException("Media object prefix must be relative and must not include dot path segments.", nameof(prefix));
        }

        var normalized = parsedPrefix.Value.TrimEnd('/');
        return parsedPrefix.Value.EndsWith(UriPathSeparator, StringComparison.Ordinal) ? normalized + UriPathSeparator : normalized;
    }

    private static string GetContentType(string objectKey) => Path.GetExtension(objectKey).ToUpperInvariant() switch
    {
        ".AVIF" => "image/avif",
        ".GIF" => "image/gif",
        ".ICO" => "image/x-icon",
        ".JPG" or ".JPEG" => "image/jpeg",
        ".PNG" => "image/png",
        ".WEBP" => "image/webp",
        _ => "application/octet-stream"
    };
}
