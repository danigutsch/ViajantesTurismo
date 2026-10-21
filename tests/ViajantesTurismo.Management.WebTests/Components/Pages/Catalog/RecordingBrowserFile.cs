namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class RecordingBrowserFile(byte[]? fileContent = null) : IBrowserFile
{
    private readonly byte[] _fileContent = fileContent ?? [0x01];

    public string Name => "tour-image.jpg";

    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    public long Size => _fileContent.LongLength;

    public string ContentType => "image/jpeg";

    public long? MaximumAllowedSize
    {
        get; private set;
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MaximumAllowedSize = maxAllowedSize;
        return new MemoryStream(_fileContent, writable: false);
    }
}
