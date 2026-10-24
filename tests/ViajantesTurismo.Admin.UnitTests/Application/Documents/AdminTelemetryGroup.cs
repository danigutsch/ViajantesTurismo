using PandaBear.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[SerialTestJustification("Admin meter instruments are process-wide, so mismatch-counter assertions must not overlap other captures.")]
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AdminTelemetryGroup
{
    public const string Name = "Admin telemetry";
}
