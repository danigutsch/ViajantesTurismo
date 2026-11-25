using System.Diagnostics;
using SharedKernel.Testing.Snapshots;

namespace ViajantesTurismo.Admin.ApiServiceTests.Reproduction;

/// <summary>Provides safe fixture metadata for validation tests that do not need an application host.</summary>
internal static class ReproductionManifestTestData
{
    private static readonly ReproductionOutcome Outcome =
        new("reproduced", "accepted", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["admin-api"] = "ready"
        });

    /// <summary>Gets the allowed values for the synthetic codec fixture.</summary>
    internal static ReproductionSpecification Specification
    {
        get;
    } = new("admin.synthetic-fixture", "0123456789abcdef0123456789abcdef01234567", [Outcome]);

    /// <summary>Creates one approved attempt.</summary>
    internal static ReproductionManifest Create() => new(
        Specification.Revision, Specification.Scenario, Guid.NewGuid(), [ActivityTraceId.CreateRandom()], Outcome,
        new DateTimeOffset(2030, 1, 2, 10, 0, 0, TimeSpan.Zero), 12);
}
