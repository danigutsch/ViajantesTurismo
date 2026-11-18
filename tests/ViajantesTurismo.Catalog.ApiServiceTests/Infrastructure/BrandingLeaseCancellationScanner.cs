using PandaBear.MalwareScanning;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Captures the intake's lease and waits for the scan token to be cancelled.</summary>
internal sealed class BrandingLeaseCancellationScanner : IMalwareScanner
{
    private readonly TaskCompletionSource<Stream> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the lease content once scanning starts.</summary>
    public Task<Stream> Entered => entered.Task;

    /// <summary>Completes when the scanner observes cancellation.</summary>
    public Task Cancelled => cancelled.Task;

    /// <inheritdoc />
    public async ValueTask<MalwareScanResult> Scan(Stream content, long length, CancellationToken ct)
    {
        await using var registration = ct.Register(() => cancelled.TrySetResult());
        entered.TrySetResult(content);
        // Await the same registration that publishes the evidence, so cancellation cannot bypass it.
        await cancelled.Task;
        ct.ThrowIfCancellationRequested();
        return MalwareScanResult.Passed;
    }
}
