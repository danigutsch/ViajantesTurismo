namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the top-level bookings OpenAPI document payload used by the consumer contract test.
/// </summary>
internal sealed record BookingsOpenApiDocumentDto(
    string OpenApi,
    BookingsOpenApiInfoDto Info,
    IReadOnlyDictionary<string, BookingsOpenApiPathDto> Paths);
