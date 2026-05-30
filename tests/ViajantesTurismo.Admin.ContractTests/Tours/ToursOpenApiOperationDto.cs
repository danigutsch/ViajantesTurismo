namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of an OpenAPI operation that the tours consumer depends on.
/// </summary>
internal sealed record ToursOpenApiOperationDto(
    string OperationId,
    ToursOpenApiRequestBodyDto? RequestBody);
