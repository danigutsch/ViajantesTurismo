using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents a referenced OpenAPI schema node consumed by the bookings contract test.
/// </summary>
/// <param name="Reference">The $ref pointer to the referenced schema.</param>
internal sealed record BookingsOpenApiSchemaDto(
    [property: JsonPropertyName("$ref")] string Reference);
