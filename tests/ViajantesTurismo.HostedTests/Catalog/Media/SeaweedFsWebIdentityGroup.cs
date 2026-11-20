using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Catalog.Media;

/// <summary>Isolates the provider fixture's container network lifecycle.</summary>
[SerialTestJustification("The provider fixture creates and removes a host network, which can interrupt Chromium sessions on the same runner.")]
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SeaweedFsWebIdentityGroup
{
    /// <summary>Gets the name of the dedicated provider test collection.</summary>
    public const string Name = "SeaweedFS workload identity";
}
