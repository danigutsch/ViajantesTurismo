using PandaBear.Testing;

namespace ViajantesTurismo.Catalog.UnitTests;

[SerialTestJustification("Telemetry tests share in-memory listener state and assert emitted activity boundaries.")]
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CatalogTelemetryGroup
{
    public const string Name = "Catalog telemetry";
}
