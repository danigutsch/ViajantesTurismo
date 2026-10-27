namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the top-level tours OpenAPI document payload used by the consumer contract test.
/// </summary>
/// <param name="OpenApi">The OpenAPI specification version string.</param>
/// <param name="Info">The document info object.</param>
/// <param name="Paths">Path items keyed by route template.</param>
internal sealed record ToursOpenApiDocumentDto(
    string OpenApi,
    ToursOpenApiInfoDto Info,
    IReadOnlyDictionary<string, ToursOpenApiPathDto> Paths);
