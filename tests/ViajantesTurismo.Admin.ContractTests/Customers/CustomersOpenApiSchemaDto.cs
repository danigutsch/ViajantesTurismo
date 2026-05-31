using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents a referenced OpenAPI schema node consumed by the customers contract test.
/// </summary>
internal sealed record CustomersOpenApiSchemaDto(
    [property: JsonPropertyName("$ref")] string? Reference,
    IReadOnlyDictionary<string, CustomersOpenApiSchemaDto>? Properties,
    IReadOnlyList<CustomersOpenApiSchemaDto>? AllOf);
