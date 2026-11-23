using PandaBear.Testing.Contracts;

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

        contract.OpenApiVersion.ShouldBe("3.2.0");
        contract.Title.ShouldBe("ViajantesTurismo.Admin.Api | customers");
        contract.ListCustomersOperationId.ShouldBe("GetCustomers");
        contract.GetCustomerPageOperationId.ShouldBe("GetCustomerPage");
        contract.SearchCustomerOptionsOperationId.ShouldBe("SearchCustomerOptions");
        contract.CustomerOptionSearchSchemaReference.ShouldBe("#/components/schemas/CustomerOptionSearchDto");
        contract.GetCustomerByIdOperationId.ShouldBe("GetCustomerById");
        contract.CreateCustomerOperationId.ShouldBe("CreateCustomer");
        contract.CreateCustomerSchemaReference.ShouldBe("#/components/schemas/CreateCustomerDto");
        contract.UpdateCustomerOperationId.ShouldBe("UpdateCustomer");
        contract.UpdateCustomerSchemaReference.ShouldBe("#/components/schemas/UpdateCustomerDto");
        contract.ImportCustomersOperationId.ShouldBe("ImportCustomers");
        contract.ImportCustomersSchemaReference.ShouldBe("#/components/schemas/IFormFile");
        contract.CommitImportOperationId.ShouldBe("CommitImportWithResolutions");
        contract.CommitImportSchemaToken.ShouldBe("multipart-object-properties:file+conflictResolutions");
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

        ContractArtifactAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
