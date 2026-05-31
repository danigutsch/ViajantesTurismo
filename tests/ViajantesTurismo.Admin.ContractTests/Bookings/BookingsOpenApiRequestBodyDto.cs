namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the request-body content map used by the bookings contract consumer.
/// </summary>
internal sealed record BookingsOpenApiRequestBodyDto(BookingsOpenApiContentMapDto Content);
