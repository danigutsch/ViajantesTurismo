using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that committed Admin OpenAPI artifacts do not drift from build-generated boundary artifacts.
/// </summary>
public sealed class AdminOpenApiArtifactDriftGuardTests
{
    [Fact]
    public void Canonical_OpenApi_Artifacts_Match_The_Generated_Boundary_Artifacts()
    {
        // Assert
        AdminOpenApiArtifactDriftGuard.AssertCanonicalArtifactsMatchGeneratedArtifacts();
    }
}
