namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the top-level customers OpenAPI document payload used by the consumer contract test.
/// </summary>
/// <param name="OpenApi">The OpenAPI specification version declared by the document.</param>
/// <param name="Info">The info object of the document.</param>
/// <param name="Paths">The path items keyed by route template.</param>
internal sealed record CustomersOpenApiDocumentDto(
    string OpenApi,
    CustomersOpenApiInfoDto Info,
    IReadOnlyDictionary<string, CustomersOpenApiPathDto> Paths);
