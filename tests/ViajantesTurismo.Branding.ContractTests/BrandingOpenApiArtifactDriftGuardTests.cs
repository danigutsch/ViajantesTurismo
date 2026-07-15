using ViajantesTurismo.Branding.ContractTests.Infrastructure;

namespace ViajantesTurismo.Branding.ContractTests;

/// <summary>
/// Verifies that canonical Branding OpenAPI artifacts do not drift from build-generated boundary artifacts.
/// </summary>
public sealed class BrandingOpenApiArtifactDriftGuardTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Canonical_branding_OpenApi_artifacts_match_the_generated_boundary_artifacts()
    {
        // Arrange
        var snapshots = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet();
        IReadOnlyList<string> expectedDrift = [];

        // Act
        var actualDrift = snapshots.GetArtifactDrift();

        // Assert
        actualDrift.ShouldBe(expectedDrift);
    }
}
