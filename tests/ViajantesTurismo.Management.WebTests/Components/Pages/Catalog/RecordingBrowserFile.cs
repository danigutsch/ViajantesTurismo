namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

/// <summary>Browser file stub that serves fixed bytes and records the maximum allowed size requested on open.</summary>
/// <param name="fileContent">File bytes to serve; the single byte 0x01 when null.</param>
/// <param name="openFailure">Exception thrown when the file is opened for reading; none when null.</param>
internal sealed class RecordingBrowserFile(byte[]? fileContent = null, Exception? openFailure = null) : IBrowserFile
{
    private readonly byte[] _fileContent = fileContent ?? [0x01];

    /// <summary>Fixed file name.</summary>
    public string Name => "tour-image.jpg";

    /// <summary>Fixed last-modified time (Unix epoch).</summary>
    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    /// <summary>Length of the file content in bytes.</summary>
    public long Size => _fileContent.LongLength;

    /// <summary>Fixed JPEG content type.</summary>
    public string ContentType => "image/jpeg";

    /// <summary>Size limit passed to the last open call, or null if never opened.</summary>
    public long? MaximumAllowedSize
    {
        get; private set;
    }

    /// <summary>Records the size limit and returns a read-only stream over the file content.</summary>
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MaximumAllowedSize = maxAllowedSize;
        if (openFailure is not null)
        {
            throw openFailure;
        }

        return new MemoryStream(_fileContent, writable: false);
    }
}
