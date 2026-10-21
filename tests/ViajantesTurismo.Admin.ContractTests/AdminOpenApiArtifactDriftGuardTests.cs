using ViajantesTurismo.Admin.ContractTests.Infrastructure;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that canonical Admin OpenAPI artifacts do not drift from build-generated boundary artifacts.
/// </summary>
public sealed class AdminOpenApiArtifactDriftGuardTests
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Canonical_OpenApi_artifacts_match_the_generated_boundary_artifacts()
    {
        // Arrange
        var snapshots = AdminOpenApiArtifactDriftGuard.CreateSnapshotSet();
        IReadOnlyList<string> expectedDrift = [];

        // Act
        var actualDrift = snapshots.GetArtifactDrift();

        // Assert
        actualDrift.ShouldBe(expectedDrift);
    }
}
