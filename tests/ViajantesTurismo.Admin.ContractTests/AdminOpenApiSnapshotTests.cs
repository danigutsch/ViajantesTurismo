namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that generated Admin OpenAPI documents match the committed canonical snapshots.
/// </summary>
public sealed class AdminOpenApiSnapshotTests
{
    [Theory]
    [InlineData("bookings")]
    [InlineData("customers")]
    [InlineData("tours")]
    public void Generated_admin_OpenApi_document_matches_the_canonical_snapshot(string boundaryName)
    {
        // Assert
        AdminOpenApiArtifactDriftGuard.AssertGeneratedArtifactMatchesCanonicalSnapshot(boundaryName);
    }
}
