using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class LocalMediaObjectStore(IOptions<LocalMediaObjectStorageOptions> storageOptions) : IMediaObjectStore
{
    private const string UriPathSeparator = "/";

    private readonly LocalMediaObjectStorageOptions options = storageOptions.Value;

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = GetSafeObjectPath(request.ObjectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Media object path must include a directory."));

        using var destination = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 81920,
            Options = FileOptions.Asynchronous
        });
        await request.Content.CopyToAsync(destination, ct).ConfigureAwait(false);

        return new MediaObjectWriteResult(
            request.ObjectKey,
            new Uri(path),
            GetPublicUri(request.ObjectKey),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    public ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var path = GetSafeObjectPath(objectKey);
        var stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = 81920,
            Options = FileOptions.Asynchronous
        });

        return ValueTask.FromResult(new MediaObjectReadResult(objectKey, stream, GetContentType(objectKey), stream.Length));
    }

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(File.Exists(GetSafeObjectPath(objectKey)));
    }

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RootPath));
        if (!Directory.Exists(root))
        {
            return ValueTask.FromResult<IReadOnlyList<string>>([]);
        }

        var normalizedPrefix = NormalizePrefix(prefix);
        var keys = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(key => key.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<string>>(keys);
    }

    public ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RootPath));
        if (!Directory.Exists(root))
        {
            return ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>([]);
        }

        var normalizedPrefix = NormalizePrefix(prefix);
        var objects = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new MediaObjectInventoryItem(
                Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                new DateTimeOffset(File.GetLastWriteTimeUtc(path))))
            .Where(item => item.ObjectKey.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .OrderBy(item => item.ObjectKey, StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>(objects);
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        throw new NotSupportedException("Local media storage does not support direct upload tickets.");
    }

    public Uri GetPublicUri(string objectKey)
    {
        _ = GetSafeObjectPath(objectKey);

        var baseUriKind = options.PublicBaseUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
        var baseUri = options.PublicBaseUri.OriginalString.EndsWith(UriPathSeparator, StringComparison.Ordinal)
            ? options.PublicBaseUri
            : new Uri(options.PublicBaseUri.OriginalString + UriPathSeparator, baseUriKind);
        var escapedKey = string.Join(
            UriPathSeparator,
            objectKey.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

        return baseUri.IsAbsoluteUri
            ? new Uri(baseUri, escapedKey)
            : new Uri(baseUri.OriginalString + escapedKey, UriKind.Relative);
    }

    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var path = GetSafeObjectPath(objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    private string GetSafeObjectPath(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("Media object key must be provided.", nameof(objectKey));
        }

        var normalizedObjectKey = objectKey.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalizedObjectKey))
        {
            throw new ArgumentException("Media object key must be relative.", nameof(objectKey));
        }

        if (normalizedObjectKey.Split(Path.DirectorySeparatorChar).Any(static segment =>
            segment.Length == 0 || segment == "." || segment == ".."))
        {
            throw new ArgumentException("Media object key must not include empty or dot path segments.", nameof(objectKey));
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RootPath));
        var path = Path.GetFullPath(Path.Combine(root, normalizedObjectKey));
        var relativePath = Path.GetRelativePath(root, path);

        return relativePath == "." || relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath)
            ? throw new ArgumentException("Media object key must stay under the configured media root.", nameof(objectKey))
            : path;
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return string.Empty;
        }

        var normalizedPrefix = prefix.Replace('\\', '/');
        if (normalizedPrefix.StartsWith('/')
            || normalizedPrefix.Split('/').Any(static segment => segment is "." or ".."))
        {
            throw new ArgumentException("Media object prefix must be relative and must not include dot path segments.", nameof(prefix));
        }

        return normalizedPrefix;
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
