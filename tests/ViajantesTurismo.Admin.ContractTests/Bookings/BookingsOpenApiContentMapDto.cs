using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the OpenAPI content-type map for JSON request payloads.
/// </summary>
/// <param name="ApplicationJson">The application/json media type, or null when not declared.</param>
internal sealed record BookingsOpenApiContentMapDto(
    [property: JsonPropertyName("application/json")] BookingsOpenApiMediaTypeDto? ApplicationJson);
