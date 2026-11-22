using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Isolates tests that inspect Npgsql spans before export processing.</summary>
[SerialTestJustification("Other hosted tests start process-wide OpenTelemetry providers whose privacy processor can remove SQL tags before the raw-span listener reads them.")]
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RawNpgsqlTracingGroup
{
    /// <summary>Gets the name of the raw Npgsql tracing test collection.</summary>
    public const string Name = "Raw Npgsql tracing";
}
