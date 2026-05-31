namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the customers contract test.
/// </summary>
internal sealed record CustomersOpenApiPathDto(
    CustomersOpenApiOperationDto? Get,
    CustomersOpenApiOperationDto? Post,
    CustomersOpenApiOperationDto? Put);
