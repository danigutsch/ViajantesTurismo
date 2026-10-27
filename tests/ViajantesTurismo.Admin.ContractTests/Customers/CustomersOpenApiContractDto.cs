namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the subset of the customers OpenAPI document that the contract consumer depends on.
/// </summary>
/// <param name="OpenApiVersion">Expected OpenAPI specification version of the document.</param>
/// <param name="Title">The API title declared in the info object.</param>
/// <param name="ListCustomersOperationId">Expected operationId of the list customers operation.</param>
/// <param name="GetCustomerByIdOperationId">Expected operationId of the get customer by id operation.</param>
/// <param name="CreateCustomerOperationId">Expected operationId of the create customer operation.</param>
/// <param name="CreateCustomerSchemaReference">Expected $ref of the create customer request schema.</param>
/// <param name="UpdateCustomerOperationId">Expected operationId of the update customer operation.</param>
/// <param name="UpdateCustomerSchemaReference">Expected $ref of the update customer request schema.</param>
/// <param name="ImportCustomersOperationId">Expected operationId of the import customers operation.</param>
/// <param name="ImportCustomersSchemaReference">Expected $ref of the import customers request schema.</param>
/// <param name="CommitImportOperationId">Expected operationId of the commit import operation.</param>
/// <param name="CommitImportSchemaToken">Expected schema name token for the commit-import request body.</param>
internal sealed record CustomersOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string ListCustomersOperationId,
    string GetCustomerByIdOperationId,
    string CreateCustomerOperationId,
    string CreateCustomerSchemaReference,
    string UpdateCustomerOperationId,
    string UpdateCustomerSchemaReference,
    string ImportCustomersOperationId,
    string ImportCustomersSchemaReference,
    string CommitImportOperationId,
    string CommitImportSchemaToken);
