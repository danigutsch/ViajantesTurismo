using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Form file stub that hands out the given stream untouched, so non-seekable sources are not repositioned.</summary>
/// <param name="stream">Stream returned by <see cref="OpenReadStream"/>.</param>
/// <param name="length">Declared file length.</param>
internal sealed class StreamFormFile(Stream stream, long length) : IFormFile
{
    /// <inheritdoc />
    public string ContentType => "text/csv";

    /// <inheritdoc />
    public string ContentDisposition => "form-data; name=\"file\"; filename=\"customers.csv\"";

    /// <inheritdoc />
    public IHeaderDictionary Headers => new HeaderDictionary();

    /// <inheritdoc />
    public long Length => length;

    /// <inheritdoc />
    public string Name => "file";

    /// <inheritdoc />
    public string FileName => "customers.csv";

    /// <inheritdoc />
    public Stream OpenReadStream() => stream;

    /// <inheritdoc />
    public void CopyTo(Stream target) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
