using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents a referenced OpenAPI schema node consumed by the tours contract test.
/// </summary>
/// <param name="Reference">The <c>$ref</c> target of the schema.</param>
internal sealed record ToursOpenApiSchemaDto(
    [property: JsonPropertyName("$ref")] string Reference);
