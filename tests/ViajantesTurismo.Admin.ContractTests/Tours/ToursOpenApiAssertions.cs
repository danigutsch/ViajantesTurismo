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

        Xunit.Assert.Equal("3.1.1", contract.OpenApiVersion);
        Xunit.Assert.Equal("ViajantesTurismo.Admin.ApiService | tours", contract.Title);
        Xunit.Assert.Equal("GetTours", contract.ListToursOperationId);
        Xunit.Assert.Equal("GetTourById", contract.GetTourByIdOperationId);
        Xunit.Assert.Equal("#/components/schemas/CreateTourDto", contract.CreateTourSchemaReference);
        Xunit.Assert.Equal("#/components/schemas/UpdateTourDto", contract.UpdateTourSchemaReference);
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

        Xunit.Assert.Equal(canonicalContract, generatedContract);
    }
}
