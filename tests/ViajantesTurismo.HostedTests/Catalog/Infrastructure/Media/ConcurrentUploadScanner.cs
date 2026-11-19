using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

/// <summary>Lets a specified number of uploads reach scanning before releasing them together.</summary>
internal sealed class ConcurrentUploadScanner(int expectedUploads, MediaUploadScanStatus status = MediaUploadScanStatus.Passed) : IMediaUploadScanner
{
    private readonly TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int scans;

    /// <summary>Gets the number of scans performed.</summary>
    public int Scans => Volatile.Read(ref scans);

    /// <inheritdoc />
    public async ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        if (Interlocked.Increment(ref scans) >= expectedUploads)
        {
            arrived.TrySetResult();
        }

        await arrived.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        return new(status);
    }
}
