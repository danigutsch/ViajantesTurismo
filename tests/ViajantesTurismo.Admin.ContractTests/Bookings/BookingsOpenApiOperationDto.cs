namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of an OpenAPI operation that the bookings consumer depends on.
/// </summary>
/// <param name="OperationId">Expected operationId of the  operation.</param>
/// <param name="RequestBody">The request body definition, or null when the operation has none.</param>
internal sealed record BookingsOpenApiOperationDto(
    string OperationId,
    BookingsOpenApiRequestBodyDto? RequestBody);
