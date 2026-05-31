namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Shared assertions for the customers OpenAPI contract slice.
/// </summary>
internal static class CustomersOpenApiAssertions
{
    /// <summary>
    /// Verifies the canonical customers contract slice values that consumers rely on.
    /// </summary>
    public static void MatchesCanonicalConsumerSlice(CustomersOpenApiContractDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        Xunit.Assert.Equal("3.1.1", contract.OpenApiVersion);
        Xunit.Assert.Equal("ViajantesTurismo.Admin.ApiService | customers", contract.Title);
        Xunit.Assert.Equal("GetCustomers", contract.ListCustomersOperationId);
        Xunit.Assert.Equal("GetCustomerById", contract.GetCustomerByIdOperationId);
        Xunit.Assert.Equal("CreateCustomer", contract.CreateCustomerOperationId);
        Xunit.Assert.Equal("#/components/schemas/CreateCustomerDto", contract.CreateCustomerSchemaReference);
        Xunit.Assert.Equal("UpdateCustomer", contract.UpdateCustomerOperationId);
        Xunit.Assert.Equal("#/components/schemas/UpdateCustomerDto", contract.UpdateCustomerSchemaReference);
        Xunit.Assert.Equal("ImportCustomers", contract.ImportCustomersOperationId);
        Xunit.Assert.Equal("#/components/schemas/IFormFile", contract.ImportCustomersSchemaReference);
        Xunit.Assert.Equal("CommitImportWithResolutions", contract.CommitImportOperationId);
        Xunit.Assert.Equal("multipart-object-allOf:file+conflictResolutions", contract.CommitImportSchemaToken);
    }

    /// <summary>
    /// Verifies that the generated customers artifact stays compatible with the canonical artifact.
    /// </summary>
    public static void MatchesGeneratedArtifact(
        CustomersOpenApiContractDto canonicalContract,
        CustomersOpenApiContractDto generatedContract)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(generatedContract);

        Xunit.Assert.Equal(canonicalContract, generatedContract);
    }
}
