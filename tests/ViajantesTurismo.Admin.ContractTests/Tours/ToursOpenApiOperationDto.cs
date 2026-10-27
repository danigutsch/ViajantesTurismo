namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of an OpenAPI operation that the tours consumer depends on.
/// </summary>
/// <param name="OperationId">The unique operation identifier.</param>
/// <param name="RequestBody">The request body definition, or null when the operation has none.</param>
internal sealed record ToursOpenApiOperationDto(
    string OperationId,
    ToursOpenApiRequestBodyDto? RequestBody);
