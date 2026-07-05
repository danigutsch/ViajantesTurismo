using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestMediaObjectStore : IMediaObjectStore
{
    public ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult(false);
    }

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<string>>([]);
    }

    public Uri GetPublicUri(string objectKey) => new($"https://cdn.example/{objectKey}");

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
