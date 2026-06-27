using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that canonical Admin OpenAPI artifacts do not drift from build-generated boundary artifacts.
/// </summary>
public sealed class AdminOpenApiArtifactDriftGuardTests
{
    [Fact]
    public void Canonical_OpenApi_artifacts_match_the_generated_boundary_artifacts()
    {
        // Assert
        AdminOpenApiArtifactDriftGuard.AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }
}
