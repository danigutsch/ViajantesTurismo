namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of the tours OpenAPI document that the contract consumer depends on.
/// </summary>
/// <param name="OpenApiVersion">The OpenAPI specification version declared by the document.</param>
/// <param name="Title">The API title from the info object.</param>
/// <param name="ListToursOperationId">The operation id of the list-tours operation.</param>
/// <param name="GetTourByIdOperationId">The operation id of the get-tour-by-id operation.</param>
/// <param name="CreateTourSchemaReference">The schema <c>$ref</c> used by the create-tour request body.</param>
/// <param name="UpdateTourSchemaReference">The schema <c>$ref</c> used by the update-tour request body.</param>
internal sealed record ToursOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string ListToursOperationId,
    string GetTourByIdOperationId,
    string CreateTourSchemaReference,
    string UpdateTourSchemaReference);
