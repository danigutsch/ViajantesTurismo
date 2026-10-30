namespace ViajantesTurismo.Management.WebTests;

/// <summary>Builds shared <see cref="FakeBrowserFile"/> instances with the names and content types the page tests expect.</summary>
internal static class TestBrowserFiles
{
    /// <summary>Creates a JPEG tour image file.</summary>
    /// <param name="content">File bytes; the single byte 0x01 when null.</param>
    /// <param name="openFailure">Exception thrown when the file is opened; none when null.</param>
    public static FakeBrowserFile TourImage(byte[]? content = null, Exception? openFailure = null)
    {
        return new FakeBrowserFile(content ?? [0x01], "tour-image.jpg", "image/jpeg") { OpenFailure = openFailure, LastModified = DateTimeOffset.UnixEpoch };
    }

    /// <summary>Creates a CSV file whose declared size can differ from its content length.</summary>
    /// <param name="content">File bytes.</param>
    /// <param name="declaredSize">Reported size; the content length when null.</param>
    public static FakeBrowserFile Csv(byte[] content, long? declaredSize = null)
    {
        return new FakeBrowserFile(content, "customers.csv", "text/csv") { DeclaredSize = declaredSize, LastModified = DateTimeOffset.UnixEpoch };
    }
}
