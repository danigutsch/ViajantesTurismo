namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the subset of the customers OpenAPI document that the contract consumer depends on.
/// </summary>
internal sealed record CustomersOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string ListCustomersOperationId,
    string GetCustomerByIdOperationId,
    string CreateCustomerSchemaReference,
    string UpdateCustomerSchemaReference,
    string ImportCustomersOperationId,
    string ImportCustomersSchemaReference,
    string CommitImportOperationId,
    string CommitImportSchemaToken);
