namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of OpenAPI path operations used by the bookings contract test.
/// </summary>
/// <param name="Get">The GET operation, or null when the path does not define one.</param>
/// <param name="Post">The POST operation, or null when the path does not define one.</param>
/// <param name="Put">The PUT operation, or null when the path does not define one.</param>
/// <param name="Patch">The PATCH operation, or null when the path does not define one.</param>
/// <param name="Delete">The DELETE operation, or null when the path does not define one.</param>
internal sealed record BookingsOpenApiPathDto(
    BookingsOpenApiOperationDto? Get,
    BookingsOpenApiOperationDto? Post,
    BookingsOpenApiOperationDto? Put,
    BookingsOpenApiOperationDto? Patch,
    BookingsOpenApiOperationDto? Delete);
