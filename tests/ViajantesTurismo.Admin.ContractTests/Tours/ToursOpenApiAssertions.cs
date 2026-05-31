namespace ViajantesTurismo.Admin.ContractTests.Tours;

internal static class ToursOpenApiAssertions
{
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

    public static void MatchesGeneratedArtifact(
        ToursOpenApiContractDto canonicalContract,
        ToursOpenApiContractDto generatedContract)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(generatedContract);

        Xunit.Assert.Equal(canonicalContract, generatedContract);
    }
}
