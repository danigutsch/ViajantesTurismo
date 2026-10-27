using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Customers;

/// <summary>
/// Represents a referenced OpenAPI schema node consumed by the customers contract test.
/// </summary>
/// <param name="Reference">The <c>$ref</c> target of the schema, or null when the schema is inline.</param>
/// <param name="Properties">Named child schemas, or null when none are declared.</param>
/// <param name="AllOf">Composed schemas that this schema extends, or null when none.</param>
internal sealed record CustomersOpenApiSchemaDto(
    [property: JsonPropertyName("$ref")] string? Reference,
    IReadOnlyDictionary<string, CustomersOpenApiSchemaDto>? Properties,
    IReadOnlyList<CustomersOpenApiSchemaDto>? AllOf);
