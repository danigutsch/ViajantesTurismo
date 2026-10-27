namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the request-body content map used by the bookings contract consumer.
/// </summary>
/// <param name="Content">The content map keyed by media type.</param>
internal sealed record BookingsOpenApiRequestBodyDto(BookingsOpenApiContentMapDto Content);
