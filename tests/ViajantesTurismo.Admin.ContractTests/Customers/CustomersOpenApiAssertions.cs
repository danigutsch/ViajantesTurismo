using SharedKernel.Testing.Assertions;
using SharedKernel.Testing.Contracts;

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

        contract.OpenApiVersion.ShouldBe("3.1.1");
        contract.Title.ShouldBe("ViajantesTurismo.Admin.ApiService | customers");
        contract.ListCustomersOperationId.ShouldBe("GetCustomers");
        contract.GetCustomerByIdOperationId.ShouldBe("GetCustomerById");
        contract.CreateCustomerOperationId.ShouldBe("CreateCustomer");
        contract.CreateCustomerSchemaReference.ShouldBe("#/components/schemas/CreateCustomerDto");
        contract.UpdateCustomerOperationId.ShouldBe("UpdateCustomer");
        contract.UpdateCustomerSchemaReference.ShouldBe("#/components/schemas/UpdateCustomerDto");
        contract.ImportCustomersOperationId.ShouldBe("ImportCustomers");
        contract.ImportCustomersSchemaReference.ShouldBe("#/components/schemas/IFormFile");
        contract.CommitImportOperationId.ShouldBe("CommitImportWithResolutions");
        contract.CommitImportSchemaToken.ShouldBe("multipart-object-allOf:file+conflictResolutions");
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
