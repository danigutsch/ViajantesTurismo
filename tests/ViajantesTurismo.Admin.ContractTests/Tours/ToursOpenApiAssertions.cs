using SharedKernel.Testing.Assertions;
using SharedKernel.Testing.Contracts;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Shared assertions for the tours OpenAPI contract slice.
/// </summary>
internal static class ToursOpenApiAssertions
{
    /// <summary>
    /// Verifies the canonical tours contract slice values that consumers rely on.
    /// </summary>
    public static void MatchesCanonicalConsumerSlice(ToursOpenApiContractDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        contract.OpenApiVersion.ShouldBe("3.1.1");
        contract.Title.ShouldBe("ViajantesTurismo.Admin.ApiService | tours");
        contract.ListToursOperationId.ShouldBe("GetTours");
        contract.GetTourByIdOperationId.ShouldBe("GetTourById");
        contract.CreateTourSchemaReference.ShouldBe("#/components/schemas/CreateTourDto");
        contract.UpdateTourSchemaReference.ShouldBe("#/components/schemas/UpdateTourDto");
    }

    /// <summary>
    /// Verifies that the generated tours artifact stays compatible with the canonical artifact.
    /// </summary>
    public static void MatchesGeneratedArtifact(
        ToursOpenApiContractDto canonicalContract,
        ToursOpenApiContractDto generatedContract)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(generatedContract);

        ContractArtifactAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
