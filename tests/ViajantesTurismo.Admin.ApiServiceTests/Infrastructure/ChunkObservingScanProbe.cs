using PandaBear.MalwareScanning;
using PandaBear.Testing;
using PandaBear.Testing.MalwareScanning;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>
/// Observes client chunk progress when each scan starts. The shared scanner captures the scanned bytes.
/// </summary>
internal sealed class ChunkObservingScanProbe
{
    private readonly Lock gate = new();
    private readonly List<int> chunksStartedAtScan = [];

    /// <summary>Creates a shared scanner with an observation hook before it touches the content.</summary>
    public ChunkObservingScanProbe()
    {
        Scanner = CallProxy.Create<IMalwareScanner>(Recorder, hooks => hooks.Scan.Before(_ => ObserveChunks()));
    }

    /// <summary>Gets the instrumented scanner for the Kestrel host.</summary>
    public IMalwareScanner Scanner
    {
        get;
    }

    /// <summary>Gets the scanner that counts the calls and captures the scanned bytes.</summary>
    public TestMalwareScanner Recorder
    {
        get;
    } = new()
    {
        ContentHandling = MalwareScanContentHandling.Capture
    };

    /// <summary>Gets or sets the source of the "client chunks started" figure read when each scan begins.</summary>
    public Func<int> ChunksStarted
    {
        get; set;
    } = () => 0;

    /// <summary>Gets the "client chunks started" figure of each scan, in the order the scans began.</summary>
    public IReadOnlyList<int> ChunksStartedAtScan
    {
        get
        {
            lock (gate)
            {
                return [.. chunksStartedAtScan];
            }
        }
    }

    private void ObserveChunks()
    {
        var chunksStarted = ChunksStarted();
        lock (gate)
        {
            chunksStartedAtScan.Add(chunksStarted);
        }
    }
}
