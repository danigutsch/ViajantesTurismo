using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the OpenAPI content-type map for JSON request payloads.
/// </summary>
/// <param name="ApplicationJson">The <c>application/json</c> media type entry, or null when absent.</param>
internal sealed record ToursOpenApiContentMapDto(
    [property: JsonPropertyName("application/json")] ToursOpenApiMediaTypeDto? ApplicationJson);
