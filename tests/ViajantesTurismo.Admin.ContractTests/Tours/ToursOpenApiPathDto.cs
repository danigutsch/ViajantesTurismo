namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the tours contract test.
/// </summary>
/// <param name="Get">The GET operation, or null when not defined.</param>
/// <param name="Post">The POST operation, or null when not defined.</param>
/// <param name="Put">The PUT operation, or null when not defined.</param>
internal sealed record ToursOpenApiPathDto(
    ToursOpenApiOperationDto? Get,
    ToursOpenApiOperationDto? Post,
    ToursOpenApiOperationDto? Put);
