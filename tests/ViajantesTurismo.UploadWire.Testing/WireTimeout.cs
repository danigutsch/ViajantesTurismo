namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>The longest wait any test step may take before the step is reported as failed.</summary>
public static class WireTimeout
{
    /// <summary>Upper bound for a server-side event the test waits on; reached only when the behavior is broken.</summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(30);
}
