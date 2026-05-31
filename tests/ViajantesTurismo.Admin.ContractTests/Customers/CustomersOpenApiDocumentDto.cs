namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the top-level customers OpenAPI document payload used by the consumer contract test.
/// </summary>
internal sealed record CustomersOpenApiDocumentDto(
    string OpenApi,
    CustomersOpenApiInfoDto Info,
    IReadOnlyDictionary<string, CustomersOpenApiPathDto> Paths);
