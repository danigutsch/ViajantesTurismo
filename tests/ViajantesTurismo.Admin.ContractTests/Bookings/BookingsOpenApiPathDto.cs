namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the bookings contract test.
/// </summary>
internal sealed record BookingsOpenApiPathDto(
    BookingsOpenApiOperationDto? Get,
    BookingsOpenApiOperationDto? Post,
    BookingsOpenApiOperationDto? Put,
    BookingsOpenApiOperationDto? Patch,
    BookingsOpenApiOperationDto? Delete);
