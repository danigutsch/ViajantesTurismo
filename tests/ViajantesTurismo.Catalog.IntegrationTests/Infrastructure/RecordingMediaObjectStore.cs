using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

internal sealed class RecordingMediaObjectStore : IMediaObjectStore
{
    public int PutCount { get; private set; }

    public ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        PutCount++;
        return ValueTask.FromResult(new MediaObjectWriteResult(
            "stored",
            new Uri("https://cdn.invalid/stored"),
            new Uri("https://cdn.invalid/stored"),
            request.ContentType,
            request.Length,
            request.Checksum));
    }

    public ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => throw new InvalidOperationException("Storage reads are not expected.");

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => ValueTask.FromResult(false);

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct) => ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask<IReadOnlyList<MediaObjectInventoryItem>> ListObjects(string prefix, CancellationToken ct) => ValueTask.FromResult<IReadOnlyList<MediaObjectInventoryItem>>([]);

    public Uri GetPublicUri(string objectKey) => new("https://cdn.invalid/stored");

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct) => throw new InvalidOperationException("Direct uploads are not expected.");

    public ValueTask Delete(string objectKey, CancellationToken ct) => ValueTask.CompletedTask;
}
