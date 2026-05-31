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
        Assert.Equal("3.1.1", contract.OpenApiVersion);
        Assert.Equal("ViajantesTurismo.Admin.ApiService | customers", contract.Title);
        Assert.Equal("GetCustomers", contract.ListCustomersOperationId);
        Assert.Equal("GetCustomerById", contract.GetCustomerByIdOperationId);
        Assert.Equal("#/components/schemas/CreateCustomerDto", contract.CreateCustomerSchemaReference);
        Assert.Equal("#/components/schemas/UpdateCustomerDto", contract.UpdateCustomerSchemaReference);
        Assert.Equal("ImportCustomers", contract.ImportCustomersOperationId);
        Assert.Equal("#/components/schemas/IFormFile", contract.ImportCustomersSchemaReference);
        Assert.Equal("CommitImportWithResolutions", contract.CommitImportOperationId);
        Assert.Equal("multipart-object-allOf:file+conflictResolutions", contract.CommitImportSchemaToken);
    }
}
