namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the customers contract test.
/// </summary>
/// <param name="Get">The GET operation, or null when the path does not define one.</param>
/// <param name="Post">The POST operation, or null when the path does not define one.</param>
/// <param name="Put">The PUT operation, or null when the path does not define one.</param>
internal sealed record CustomersOpenApiPathDto(
    CustomersOpenApiOperationDto? Get,
    CustomersOpenApiOperationDto? Post,
    CustomersOpenApiOperationDto? Put);
