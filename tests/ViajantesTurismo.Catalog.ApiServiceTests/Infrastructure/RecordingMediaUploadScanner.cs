using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Media upload scanner that records what it received and reports the content clean.</summary>
/// <param name="recorder">The recorder that captures each scan.</param>
internal sealed class RecordingMediaUploadScanner(ScanRecorder recorder) : IMediaUploadScanner
{
    /// <inheritdoc />
    public async ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await recorder.Record(request.Content, ct).ConfigureAwait(false);
        return MediaUploadScanResult.Passed;
    }
}
