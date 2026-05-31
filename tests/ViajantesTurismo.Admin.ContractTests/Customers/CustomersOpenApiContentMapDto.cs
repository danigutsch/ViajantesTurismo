using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the OpenAPI content-type map used by the customers contract consumer.
/// </summary>
internal sealed record CustomersOpenApiContentMapDto(
    [property: JsonPropertyName("application/json")] CustomersOpenApiMediaTypeDto? ApplicationJson,
    [property: JsonPropertyName("multipart/form-data")] CustomersOpenApiMediaTypeDto? MultipartFormData);
