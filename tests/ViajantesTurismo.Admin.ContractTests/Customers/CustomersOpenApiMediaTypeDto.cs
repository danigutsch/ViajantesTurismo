namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the JSON or multipart media-type payload consumed from the customers OpenAPI document.
/// </summary>
/// <param name="Schema">The schema node of the media type.</param>
internal sealed record CustomersOpenApiMediaTypeDto(CustomersOpenApiSchemaDto Schema);
