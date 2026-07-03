using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class StubMediaUploadScanner(MediaUploadScanResult result, Exception? exception = null) : IMediaUploadScanner
{
    public MediaUploadScanRequest? LastRequest { get; private set; }

    public int ScanCount { get; private set; }

    public ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastRequest = request;
        ScanCount++;

        if (exception is not null)
        {
            throw exception;
        }

        return ValueTask.FromResult(result);
    }
}
