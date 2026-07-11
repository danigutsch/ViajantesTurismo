using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class SeaweedFsMediaObjectStore(
    IAmazonS3 client,
    IOptions<SeaweedFsMediaObjectStorageOptions> storageOptions) : IMediaObjectStore
{
    private const string Separator = "/";
    private readonly SeaweedFsMediaObjectStorageOptions options = storageOptions.Value;
    private readonly Lock bucketInitializationLock = new();
    private Task? bucketInitialization;

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateKey(request.ObjectKey);
        await EnsureBucketProvisioned(ct).ConfigureAwait(false);

        var putRequest = new PutObjectRequest
        {
            BucketName = options.Bucket,
            Key = request.ObjectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            Headers = { ContentLength = request.Length }
        };
        if (request.Metadata is not null)
        {
            foreach (var (key, value) in request.Metadata)
            {
                putRequest.Metadata[key] = value;
            }
        }

        if (request.Checksum is not null)
        {
            putRequest.Metadata["checksum"] = request.Checksum;
        }

        await client.PutObjectAsync(putRequest, ct).ConfigureAwait(false);

        return new MediaObjectWriteResult(request.ObjectKey, GetPrivateUri(request.ObjectKey), GetPublicUri(request.ObjectKey), request.ContentType, request.Length, request.Checksum);
    }

    public async ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ValidateKey(objectKey);
        await EnsureBucketProvisioned(ct).ConfigureAwait(false);

        var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = options.Bucket, Key = objectKey }, ct).ConfigureAwait(false);
        var checksum = GetMetadataValue(response.Metadata, "checksum");
        return new MediaObjectReadResult(objectKey, new SeaweedFsObjectResponseStream(response), response.Headers.ContentType, response.Headers.ContentLength, checksum);
    }

    public async ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ValidateKey(objectKey);
        await EnsureBucketProvisioned(ct).ConfigureAwait(false);

        try
        {
            _ = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = options.Bucket, Key = objectKey }, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        var objects = await List(prefix, ct).ConfigureAwait(false);
        return objects.Select(static item => item.Key).ToArray();
    }

    public async ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct)
    {
        var objects = await List(prefix, ct).ConfigureAwait(false);
        return objects.Select(static item => new MediaObjectInventoryItem(item.Key, ToUtcDateTimeOffset(item.LastModified.GetValueOrDefault()))).ToArray();
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        throw new NotSupportedException("SeaweedFS media storage does not support direct upload tickets.");
    }

    public Uri GetPublicUri(string objectKey)
    {
        ValidateKey(objectKey);
        var baseUriKind = options.PublicBaseUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
        var baseUri = options.PublicBaseUri.OriginalString.EndsWith(Separator, StringComparison.Ordinal)
            ? options.PublicBaseUri
            : new Uri(options.PublicBaseUri.OriginalString + Separator, baseUriKind);
        var escapedKey = string.Join(Separator, objectKey.Split(Separator).Select(Uri.EscapeDataString));
        return baseUri.IsAbsoluteUri ? new Uri(baseUri, escapedKey) : new Uri(baseUri.OriginalString + escapedKey, UriKind.Relative);
    }

    public async ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ValidateKey(objectKey);
        await EnsureBucketProvisioned(ct).ConfigureAwait(false);
        await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = options.Bucket, Key = objectKey }, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<S3Object>> List(string prefix, CancellationToken ct)
    {
        ValidatePrefix(prefix);
        await EnsureBucketProvisioned(ct).ConfigureAwait(false);
        var objects = new List<S3Object>();
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = options.Bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, ct).ConfigureAwait(false);
            objects.AddRange(response.S3Objects);
            continuationToken = response.IsTruncated.GetValueOrDefault() ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return objects;
    }

    private Uri GetPrivateUri(string objectKey)
    {
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("SeaweedFS endpoint must be configured.");
        var baseUri = endpoint.OriginalString.EndsWith(Separator, StringComparison.Ordinal)
            ? endpoint
            : new Uri(endpoint.OriginalString + Separator, UriKind.Absolute);
        var escapedKey = string.Join(Separator, objectKey.Split(Separator).Select(Uri.EscapeDataString));
        return new Uri(baseUri, $"{options.Bucket}/{escapedKey}");
    }

    private static string? GetMetadataValue(MetadataCollection metadata, string key)
    {
        if (metadata.Keys.Contains(key, StringComparer.Ordinal))
        {
            return metadata[key];
        }

        var headerKey = $"x-amz-meta-{key}";
        return metadata.Keys.Contains(headerKey, StringComparer.Ordinal) ? metadata[headerKey] : null;
    }

    private static DateTimeOffset ToUtcDateTimeOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return new DateTimeOffset(utc);
    }

    private async Task EnsureBucketProvisioned(CancellationToken ct)
    {
        if (!options.AutoProvisionBucket)
        {
            return;
        }

        Task initialization;
        lock (bucketInitializationLock)
        {
            initialization = bucketInitialization ??= EnsureBucketCore();
        }

        try
        {
            await initialization.WaitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            if (initialization.IsFaulted || initialization.IsCanceled)
            {
                lock (bucketInitializationLock)
                {
                    if (ReferenceEquals(bucketInitialization, initialization))
                    {
                        bucketInitialization = null;
                    }
                }
            }

            throw;
        }
    }

    private async Task EnsureBucketCore()
    {
        using var timeout = new CancellationTokenSource(options.BucketProvisioningTimeout);
        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = options.Bucket }, timeout.Token).ConfigureAwait(false);
        }
        catch (AmazonS3Exception exception) when (string.Equals(exception.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.Ordinal))
        {
            // A concurrent process created the application-owned bucket first.
        }
    }

    private static void ValidateKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)
            || objectKey.StartsWith(Separator, StringComparison.Ordinal)
            || objectKey.Contains('\\', StringComparison.Ordinal)
            || objectKey.Split(Separator).Any(static segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new ArgumentException("Media object key must be a relative slash-delimited path without dot segments.", nameof(objectKey));
        }
    }

    private static void ValidatePrefix(string prefix)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            ValidateKey(prefix.TrimEnd('/'));
        }
    }
}
