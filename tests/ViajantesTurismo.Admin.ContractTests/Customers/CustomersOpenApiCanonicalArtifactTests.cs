using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Verifies that the canonical customers OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class CustomersOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_the_customers_openApi_document_through_the_canonical_artifact()
    {
        // Act
        var contract = await CustomersOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        CustomersOpenApiAssertions.MatchesCanonicalConsumerSlice(contract);
    }
}
