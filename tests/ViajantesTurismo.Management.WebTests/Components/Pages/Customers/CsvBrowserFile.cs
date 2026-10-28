namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>Browser file stub for CSV imports whose declared size can differ from the bytes the stream serves.</summary>
/// <param name="fileContent">Bytes served by the read stream.</param>
/// <param name="declaredSize">Value reported by <see cref="Size"/>; the content length when null.</param>
internal sealed class CsvBrowserFile(byte[] fileContent, long? declaredSize = null) : IBrowserFile
{
    /// <summary>Fixed CSV file name.</summary>
    public string Name => "customers.csv";

    /// <summary>Fixed last-modified time (Unix epoch).</summary>
    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    /// <summary>Declared file size in bytes.</summary>
    public long Size => declaredSize ?? fileContent.LongLength;

    /// <summary>Fixed CSV content type.</summary>
    public string ContentType => "text/csv";

    /// <summary>Size limit passed to the last open call, or null if never opened.</summary>
    public long? MaximumAllowedSize
    {
        get; private set;
    }

    /// <summary>Records the size limit and returns a read-only stream, failing like the framework when the size exceeds the limit.</summary>
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        MaximumAllowedSize = maxAllowedSize;
        if (Size > maxAllowedSize)
        {
            throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.");
        }

        return new MemoryStream(fileContent, writable: false);
    }
}
