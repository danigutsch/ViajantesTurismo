namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of an OpenAPI operation that the bookings consumer depends on.
/// </summary>
internal sealed record BookingsOpenApiOperationDto(
    string OperationId,
    BookingsOpenApiRequestBodyDto? RequestBody);
