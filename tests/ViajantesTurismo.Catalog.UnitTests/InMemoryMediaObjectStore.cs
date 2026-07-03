using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class InMemoryMediaObjectStore : IMediaObjectStore
{
    private readonly Dictionary<string, MediaObjectWriteRequest> objects = [];

    public IReadOnlyCollection<string> ObjectKeys => objects.Keys;

    public async ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var content = new MemoryStream();
        await request.Content.CopyToAsync(content, ct);
        objects[request.ObjectKey] = request with { Content = new MemoryStream(content.ToArray()), Length = content.Length };

        return new MediaObjectWriteResult(
            request.ObjectKey,
            new Uri($"file:///media/{request.ObjectKey}"),
            new Uri($"https://cdn.example/{request.ObjectKey}"),
            request.ContentType,
            content.Length,
            request.Checksum);
    }

    public async ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var request = objects[objectKey];
        request.Content.Position = 0;
        using var source = new MemoryStream();
        await request.Content.CopyToAsync(source, ct);

        return new MediaObjectReadResult(
            objectKey,
            new MemoryStream(source.ToArray()),
            request.ContentType,
            request.Length,
            request.Checksum);
    }

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    public ValueTask Delete(string objectKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        objects.Remove(objectKey);

        return ValueTask.CompletedTask;
    }
}
