using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Verifies that the canonical tours OpenAPI artifact preserves the consumer-owned contract slice.
/// </summary>
public sealed class ToursOpenApiCanonicalArtifactTests
{
    [Fact]
    public async Task Reads_The_Tours_OpenApi_Document_Through_The_Canonical_Artifact()
    {
        // Act
        var contract = await ToursOpenApiDocumentClient.GetContract(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("3.1.1", contract.OpenApiVersion);
        Assert.Equal("ViajantesTurismo.Admin.ApiService | tours", contract.Title);
        Assert.Equal("GetTours", contract.ListToursOperationId);
        Assert.Equal("GetTourById", contract.GetTourByIdOperationId);
        Assert.Equal("#/components/schemas/CreateTourDto", contract.CreateTourSchemaReference);
        Assert.Equal("#/components/schemas/UpdateTourDto", contract.UpdateTourSchemaReference);
    }
}
