using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

/// <summary>Verifies the original test photo still reaches the scanner unchanged.</summary>
internal sealed class ExactGalleryUploadScanner(ReadOnlyMemory<byte> expected, Task? release = null) : IMediaUploadScanner
{
    private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals when the upload reaches its scan.</summary>
    public Task Started => started.Task;

    /// <summary>Gets the number of completed scans.</summary>
    public int Scans
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public async ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        request.Content.Position.ShouldBe(0);
        request.Length.ShouldBe(expected.Length);
        var received = new byte[expected.Length];
        await request.Content.ReadExactlyAsync(received, ct);
        received.AsSpan().SequenceEqual(expected.Span).ShouldBeTrue();
        started.TrySetResult();
        if (release is not null)
        {
            await release.WaitAsync(ct);
        }

        Scans++;
        return MediaUploadScanResult.Passed;
    }
}
