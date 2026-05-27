namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the tours contract test.
/// </summary>
internal sealed record ToursOpenApiPathDto(
    ToursOpenApiOperationDto? Get,
    ToursOpenApiOperationDto? Post,
    ToursOpenApiOperationDto? Put);
