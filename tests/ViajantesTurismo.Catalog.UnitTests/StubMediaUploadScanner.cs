using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Upload scanner stub that records requests and returns a fixed result or throws.</summary>
/// <param name="result">Result returned when no exception is configured.</param>
/// <param name="exception">Exception thrown on every scan when not null.</param>
internal sealed class StubMediaUploadScanner(MediaUploadScanResult result, Exception? exception = null) : IMediaUploadScanner
{
    /// <summary>Gets the last scan request received.</summary>
    public MediaUploadScanRequest? LastRequest
    {
        get; private set;
    }

    /// <summary>Gets how many scans were requested.</summary>
    public int ScanCount
    {
        get; private set;
    }

    /// <summary>Records the request, then returns the fixed result or throws the configured exception.</summary>
    public ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        LastRequest = request;
        ScanCount++;

        return exception is null ? ValueTask.FromResult(result) : throw exception;
    }
}
