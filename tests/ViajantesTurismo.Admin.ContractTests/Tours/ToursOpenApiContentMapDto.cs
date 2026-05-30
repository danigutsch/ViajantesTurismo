using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the OpenAPI content-type map for JSON request payloads.
/// </summary>
internal sealed record ToursOpenApiContentMapDto(
    [property: JsonPropertyName("application/json")] ToursOpenApiMediaTypeDto? ApplicationJson);
