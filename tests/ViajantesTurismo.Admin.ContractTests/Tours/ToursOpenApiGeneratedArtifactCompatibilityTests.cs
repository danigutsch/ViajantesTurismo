using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Verifies that the generated tours artifact remains compatible with the canonical artifact.
/// </summary>
public sealed class ToursOpenApiGeneratedArtifactCompatibilityTests
{
    [Fact]
    public async Task Generated_Tours_Artifact_Matches_The_Canonical_Artifact_Contract_Slice()
    {
        // Arrange
        var generatedContract = await ToursOpenApiDocumentClient.GetGeneratedContract(TestContext.Current.CancellationToken);
        var canonicalContract = await ToursOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        ToursOpenApiAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
