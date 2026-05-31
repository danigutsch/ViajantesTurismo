using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Verifies that the generated bookings artifact remains compatible with the canonical artifact.
/// </summary>
public sealed class BookingsOpenApiGeneratedArtifactCompatibilityTests
{
    [Fact]
    public async Task Generated_Bookings_Artifact_Matches_The_Canonical_Artifact_Contract_Slice()
    {
        // Arrange
        var generatedContract = await BookingsOpenApiDocumentClient.GetGeneratedContract(TestContext.Current.CancellationToken);
        var canonicalContract = await BookingsOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        BookingsOpenApiAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
