using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class NoOpMediaUploadScanner : IMediaUploadScanner
{
    public ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new MediaUploadScanResult(MediaUploadScanStatus.Disabled));
    }
}
