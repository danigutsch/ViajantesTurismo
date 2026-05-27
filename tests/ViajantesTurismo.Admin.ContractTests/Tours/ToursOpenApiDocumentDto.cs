namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the top-level tours OpenAPI document payload used by the consumer contract test.
/// </summary>
internal sealed record ToursOpenApiDocumentDto(
    string OpenApi,
    ToursOpenApiInfoDto Info,
    IReadOnlyDictionary<string, ToursOpenApiPathDto> Paths);
