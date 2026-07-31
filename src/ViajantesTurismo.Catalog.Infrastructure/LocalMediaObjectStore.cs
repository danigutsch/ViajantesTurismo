using Microsoft.Extensions.Options;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class LocalMediaObjectStore(IOptions<LocalMediaObjectStorageOptions> storageOptions) : IMediaObjectStore
{
    private const string UriPathSeparator = "/";
    private const string TemporaryFilePrefix = ".viajantes-";
    private const string TemporaryFileSuffix = ".tmp";

    private readonly LocalMediaObjectStorageOptions options = storageOptions.Value;

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = GetSafeObjectPath(request.ObjectKey);
        if (IsTemporaryArtifact(path))
        {
            throw new ArgumentException("The object key uses a reserved temporary-file name.", nameof(request));
        }

        var directoryPath = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Media object path must include a directory.");
        Directory.CreateDirectory(directoryPath);
        var temporaryPath = Path.Combine(
            directoryPath,
            $"{TemporaryFilePrefix}{Guid.CreateVersion7():N}{TemporaryFileSuffix}");

        try
        {
            var destination = new FileStream(temporaryPath, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 81920,
                Options = FileOptions.Asynchronous
            });
            await using (destination.ConfigureAwait(false))
            {
                await request.Content.CopyToAsync(destination, ct).ConfigureAwait(false);
                await destination.FlushAsync(ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            DeleteTemporaryFileAfterFailure(temporaryPath);
            throw;
        }

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
            Share = FileShare.Read | FileShare.Delete,
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
            .Where(path => !IsTemporaryArtifact(path))
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
            .Where(path => !IsTemporaryArtifact(path))
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
            objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

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
        if (!ObjectStorageKeyValidator.IsValidRelativeKey(objectKey, CatalogDomainLimits.MaxMediaObjectKeyLength))
        {
            throw new ArgumentException("Media object key must be a relative slash-delimited path without dot segments.", nameof(objectKey));
        }

        var normalizedObjectKey = objectKey.Replace('/', Path.DirectorySeparatorChar);

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

        var normalizedPrefix = prefix.TrimEnd('/');
        if (!ObjectStorageKeyValidator.IsValidRelativeKey(normalizedPrefix, CatalogDomainLimits.MaxMediaObjectKeyLength))
        {
            throw new ArgumentException("Media object prefix must be relative and must not include dot path segments.", nameof(prefix));
        }

        return prefix.EndsWith(UriPathSeparator, StringComparison.Ordinal) ? normalizedPrefix + UriPathSeparator : normalizedPrefix;
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

    private static bool IsTemporaryArtifact(string path)
    {
        var fileName = Path.GetFileName(path);
        var expectedLength = TemporaryFilePrefix.Length + 32 + TemporaryFileSuffix.Length;
        if (fileName.Length != expectedLength ||
            !fileName.StartsWith(TemporaryFilePrefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(TemporaryFileSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        return Guid.TryParseExact(fileName.AsSpan(TemporaryFilePrefix.Length, 32), "N", out _);
    }

    private static void DeleteTemporaryFileAfterFailure(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (IOException)
        {
            // Preserve the primary write failure; reserved artifacts remain hidden from object inventory.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the primary write failure; reserved artifacts remain hidden from object inventory.
        }
    }
}
