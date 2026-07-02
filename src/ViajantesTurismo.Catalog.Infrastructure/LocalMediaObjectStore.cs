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

        using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await request.Content.CopyToAsync(destination, ct).ConfigureAwait(false);

        return new MediaObjectWriteResult(
            request.ObjectKey,
            new Uri(path),
            CreatePublicUri(request.ObjectKey),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        throw new NotSupportedException("Local media storage does not support direct upload tickets.");
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

        if (relativePath == "." || relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Media object key must stay under the configured media root.", nameof(objectKey));
        }

        return path;
    }

    private Uri CreatePublicUri(string objectKey)
    {
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
}
