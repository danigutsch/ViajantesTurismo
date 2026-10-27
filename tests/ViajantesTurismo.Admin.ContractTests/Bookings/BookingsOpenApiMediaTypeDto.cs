namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the JSON media-type payload consumed from the bookings OpenAPI document.
/// </summary>
/// <param name="Schema">The schema node of the media type.</param>
internal sealed record BookingsOpenApiMediaTypeDto(BookingsOpenApiSchemaDto Schema);
