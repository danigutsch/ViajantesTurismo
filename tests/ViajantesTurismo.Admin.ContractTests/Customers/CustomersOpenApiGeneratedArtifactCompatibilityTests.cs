using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Verifies that the generated customers artifact remains compatible with the canonical artifact.
/// </summary>
public sealed class CustomersOpenApiGeneratedArtifactCompatibilityTests
{
    [Fact]
    public async Task Generated_customers_artifact_matches_the_canonical_artifact_contract_slice()
    {
        // Arrange
        var generatedContract = await CustomersOpenApiDocumentClient.GetGeneratedContract(TestContext.Current.CancellationToken);
        var canonicalContract = await CustomersOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        CustomersOpenApiAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
