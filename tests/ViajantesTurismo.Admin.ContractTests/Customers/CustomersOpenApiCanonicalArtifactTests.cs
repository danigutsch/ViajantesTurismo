using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Verifies that the canonical customers OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class CustomersOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_The_Customers_OpenApi_Document_Through_The_Canonical_Artifact()
    {
        // Act
        var contract = await CustomersOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        CustomersOpenApiAssertions.MatchesCanonicalConsumerSlice(contract);
    }
}
