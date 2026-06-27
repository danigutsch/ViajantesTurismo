using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Verifies that the generated tours artifact remains compatible with the canonical artifact.
/// </summary>
public sealed class ToursOpenApiGeneratedArtifactCompatibilityTests
{
    [Fact]
    public async Task Generated_tours_artifact_matches_the_canonical_artifact_contract_slice()
    {
        // Arrange
        var generatedContract = await ToursOpenApiDocumentClient.GetGeneratedContract(TestContext.Current.CancellationToken);
        var canonicalContract = await ToursOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        ToursOpenApiAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
