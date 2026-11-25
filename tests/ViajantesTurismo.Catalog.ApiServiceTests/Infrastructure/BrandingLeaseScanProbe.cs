using SharedKernel.MalwareScanning;
using SharedKernel.Testing;
using SharedKernel.Testing.MalwareScanning;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Observes the intake lease and cancellation token as the shared scanner enters its gate.</summary>
internal sealed class BrandingLeaseScanProbe
{
    private readonly MalwareScanGate gate = new();
    private readonly TaskCompletionSource<(Stream Lease, CancellationToken ScanCancellation)> input =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Creates a scanner held at a gate and records the original scan arguments.</summary>
    public BrandingLeaseScanProbe()
    {
        Scanner = CallProxy.Create<IMalwareScanner>(
            TestMalwareScanner.Blocking(gate),
            hooks => hooks.Scan.Before(call => input.TrySetResult((call.Content, call.Ct))));
    }

    /// <summary>Gets the instrumented scanner for the Kestrel host.</summary>
    public IMalwareScanner Scanner
    {
        get;
    }

    /// <summary>Waits until the scanner has started and returns the intake lease and scan token.</summary>
    public async Task<(Stream Lease, CancellationToken ScanCancellation)> WaitForScan(CancellationToken ct)
    {
        await gate.Entered.WaitAsync(WireTimeout.Default, ct);
        return await input.Task.WaitAsync(WireTimeout.Default, ct);
    }
}
