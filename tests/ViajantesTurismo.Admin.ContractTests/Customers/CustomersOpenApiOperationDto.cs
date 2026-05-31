namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the subset of an OpenAPI operation that the customers consumer depends on.
/// </summary>
internal sealed record CustomersOpenApiOperationDto(
    string OperationId,
    CustomersOpenApiRequestBodyDto? RequestBody);
