using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Verifies that the canonical tours OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class ToursOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_the_tours_openApi_document_through_the_canonical_artifact()
    {
        // Act
        var contract = await ToursOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        ToursOpenApiAssertions.MatchesCanonicalConsumerSlice(contract);
    }
}
