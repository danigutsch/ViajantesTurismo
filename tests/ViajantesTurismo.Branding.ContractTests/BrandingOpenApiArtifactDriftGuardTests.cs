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

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Branding_mutation_keeps_its_published_response_contract()
    {
        // Arrange
        var document = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot("branding").AsObject();
        var operation = document["paths"].ShouldNotBeNull().AsObject()
            ["/api/v1/branding/settings"].ShouldNotBeNull().AsObject()
            ["put"].ShouldNotBeNull().AsObject();

        // Act
        var responses = operation["responses"].ShouldNotBeNull().AsObject();
        var responseStatuses = responses.Select(response => response.Key).Order(StringComparer.Ordinal).ToArray();

        // Assert
        responseStatuses.ShouldBe(["200", "401", "403"]);
        responses["200"].ShouldNotBeNull().AsObject().ContainsKey("content").ShouldBeFalse();
    }
}
