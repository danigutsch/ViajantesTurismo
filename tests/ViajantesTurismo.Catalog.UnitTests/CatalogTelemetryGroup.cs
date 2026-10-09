namespace ViajantesTurismo.Catalog.UnitTests;

[SharedKernel.Testing.SerialTestJustification("Telemetry tests share in-memory listener state and assert emitted activity boundaries.")]
[CollectionDefinition("Catalog telemetry", DisableParallelization = true)]
public sealed class CatalogTelemetryGroup
{
}
