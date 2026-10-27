using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents the OpenAPI content-type map used by the customers contract consumer.
/// </summary>
/// <param name="ApplicationJson">The application/json media type, or null when not declared.</param>
/// <param name="MultipartFormData">The multipart/form-data media type, or null when not declared.</param>
internal sealed record CustomersOpenApiContentMapDto(
    [property: JsonPropertyName("application/json")] CustomersOpenApiMediaTypeDto? ApplicationJson,
    [property: JsonPropertyName("multipart/form-data")] CustomersOpenApiMediaTypeDto? MultipartFormData);
