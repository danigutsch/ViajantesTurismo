namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the request-body content map used by the customers contract consumer.
/// </summary>
/// <param name="Content">The content map keyed by media type.</param>
internal sealed record CustomersOpenApiRequestBodyDto(CustomersOpenApiContentMapDto Content);
