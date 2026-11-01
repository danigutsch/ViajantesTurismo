using System.Buffers;
using System.Security.Cryptography;
using PandaBear.ObjectStorage;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Stores media objects as files under the configured local root, for development and tests.</summary>
internal sealed class LocalMediaObjectStore(IOptions<LocalMediaObjectStorageOptions> storageOptions) : IObjectStore
{
    private const string UriPathSeparator = "/";
    private const string TemporaryFilePrefix = ".viajantes-";
    private const string TemporaryFileSuffix = ".tmp";
    private const string Sha256ChecksumPrefix = "sha256:";
    private const int CopyBufferSize = 81920;

    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");

    private readonly LocalMediaObjectStorageOptions options = storageOptions.Value;

    /// <summary>Writes the object to a temporary file and moves it into place, replacing any existing object.</summary>
    public async ValueTask<ObjectWriteResult> Put(ObjectWriteRequest request, CancellationToken ct)
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
                await CopyVerified(request, destination, ct).ConfigureAwait(false);
                await destination.FlushAsync(ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
            try
            {
                File.Move(temporaryPath, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                File.Replace(temporaryPath, path, destinationBackupFileName: null);
            }
        }
        catch
        {
            DeleteTemporaryFileAfterFailure(temporaryPath);
            throw;
        }

        return new ObjectWriteResult(
            request.ObjectKey,
            new Uri(path),
            GetPublicUri(request.ObjectKey),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    /// <summary>
    /// Copies exactly the declared length from the start of the content, then confirms the content has no more bytes and that the
    /// SHA-256 matches the request checksum when one was supplied.
    /// </summary>
    private static async ValueTask CopyVerified(ObjectWriteRequest request, Stream destination, CancellationToken ct)
    {
        if (request.Length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Length, "Object length must not be negative.");
        }

        var expectedHash = ParseSha256(request.Checksum);
        var content = request.Content;
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            var remaining = request.Length;
            while (remaining > 0)
            {
                var read = await content.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), ct).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new InvalidDataException("The content is shorter than the declared object length.");
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                remaining -= read;
            }

            if (await content.ReadAsync(buffer.AsMemory(0, 1), ct).ConfigureAwait(false) != 0)
            {
                throw new InvalidDataException("The content is longer than the declared object length.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (expectedHash is not null && !CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), expectedHash))
        {
            throw new InvalidDataException("The content does not match the supplied checksum.");
        }
    }

    private static byte[]? ParseSha256(string? checksum)
    {
        if (string.IsNullOrEmpty(checksum))
        {
            return null;
        }

        var hex = checksum.StartsWith(Sha256ChecksumPrefix, StringComparison.OrdinalIgnoreCase)
            ? checksum.AsSpan(Sha256ChecksumPrefix.Length)
            : default;
        if (hex.Length != SHA256.HashSizeInBytes * 2 || hex.IndexOfAnyExcept(HexDigits) >= 0)
        {
            throw new ArgumentException("The checksum must be sha256: followed by 64 hexadecimal digits.", nameof(checksum));
        }

        return Convert.FromHexString(hex);
    }

    /// <summary>Opens the stored object for reading.</summary>
    public ValueTask<ObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
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

        return ValueTask.FromResult(new ObjectReadResult(objectKey, stream, GetContentType(objectKey), stream.Length));
    }

    /// <summary>Returns whether a file exists for the object key.</summary>
    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(File.Exists(GetSafeObjectPath(objectKey)));
    }

    /// <summary>Lists the keys of stored objects under the prefix as slash-delimited relative paths.</summary>
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

    /// <summary>Lists stored objects under the prefix with their inventory metadata.</summary>
    public ValueTask<IReadOnlyList<ObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RootPath));
        if (!Directory.Exists(root))
        {
            return ValueTask.FromResult<IReadOnlyList<ObjectInventoryItem>>([]);
        }

        var normalizedPrefix = NormalizePrefix(prefix);
        var objects = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !IsTemporaryArtifact(path))
            .Select(path => new ObjectInventoryItem(
                Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                new DateTimeOffset(File.GetLastWriteTimeUtc(path))))
            .Where(item => item.ObjectKey.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .OrderBy(item => item.ObjectKey, StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<ObjectInventoryItem>>(objects);
    }

    /// <summary>Always throws because local storage does not support direct upload tickets.</summary>
    public ValueTask<ObjectUploadTicket> CreateUploadUrl(ObjectUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        throw new NotSupportedException("Local media storage does not support direct upload tickets.");
    }

    /// <summary>Builds the public URI for the object key from the configured public base URI.</summary>
    public Uri GetPublicUri(string objectKey)
    {
        var key = ParseObjectKey(objectKey);
        _ = GetSafeObjectPath(key);

        var baseUriKind = options.PublicBaseUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
        var baseUri = options.PublicBaseUri.OriginalString.EndsWith(UriPathSeparator, StringComparison.Ordinal)
            ? options.PublicBaseUri
            : new Uri(options.PublicBaseUri.OriginalString + UriPathSeparator, baseUriKind);
        var escapedKey = key.ToEscapedPath();

        return baseUri.IsAbsoluteUri
            ? new Uri(baseUri, escapedKey)
            : new Uri(baseUri.OriginalString + escapedKey, UriKind.Relative);
    }

    /// <summary>Deletes the stored object file if it exists.</summary>
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

    private static ObjectStorageKey ParseObjectKey(string objectKey)
    {
        if (!ObjectStorageKey.TryFrom(objectKey, out var key, CatalogDomainLimits.MaxMediaObjectKeyLength))
        {
            throw new ArgumentException("Media object key must be a relative slash-delimited path without dot segments.", nameof(objectKey));
        }

        return key;
    }

    private string GetSafeObjectPath(string objectKey) => GetSafeObjectPath(ParseObjectKey(objectKey));

    private string GetSafeObjectPath(ObjectStorageKey objectKey)
    {
        var normalizedObjectKey = objectKey.Value.Replace('/', Path.DirectorySeparatorChar);

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

        if (!ObjectStoragePrefix.TryFrom(prefix, out var parsedPrefix, CatalogDomainLimits.MaxMediaObjectKeyLength))
        {
            throw new ArgumentException("Media object prefix must be relative and must not include dot path segments.", nameof(prefix));
        }

        var normalizedPrefix = parsedPrefix.Value.TrimEnd('/');
        return parsedPrefix.Value.EndsWith(UriPathSeparator, StringComparison.Ordinal) ? normalizedPrefix + UriPathSeparator : normalizedPrefix;
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

    /// <summary>Preserves the primary write failure; reserved artifacts remain hidden from object inventory.</summary>
    private static void DeleteTemporaryFileAfterFailure(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
