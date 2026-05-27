namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of the tours OpenAPI document that the contract consumer depends on.
/// </summary>
internal sealed record ToursOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string ListToursOperationId,
    string GetTourByIdOperationId,
    string CreateTourSchemaReference,
    string UpdateTourSchemaReference);
