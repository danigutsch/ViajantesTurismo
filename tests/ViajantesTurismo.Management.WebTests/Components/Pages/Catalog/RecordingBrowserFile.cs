namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class RecordingBrowserFile : IBrowserFile
{
    public string Name => "tour-image.jpg";

    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    public long Size => 1;

    public string ContentType => "image/jpeg";

    public long? MaximumAllowedSize { get; private set; }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MaximumAllowedSize = maxAllowedSize;
        return new MemoryStream([0x01]);
    }
}
