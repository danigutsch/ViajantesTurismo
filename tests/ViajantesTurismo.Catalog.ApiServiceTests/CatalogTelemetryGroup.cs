using PandaBear.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[SerialTestJustification("Catalog metrics use a process-wide meter; endpoint tests that count review decisions must not run beside other tests that call the review endpoint.")]
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CatalogTelemetryGroup
{
    public const string Name = "Catalog telemetry";
}
