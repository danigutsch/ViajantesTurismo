namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of the bookings OpenAPI document that the contract consumer depends on.
/// </summary>
internal sealed record BookingsOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string ListBookingsOperationId,
    string GetBookingByIdOperationId,
    string CreateBookingOperationId,
    string CreateBookingSchemaReference,
    string DeleteBookingOperationId,
    string GetBookingsByTourIdOperationId,
    string GetBookingsByCustomerIdOperationId,
    string UpdateBookingDiscountOperationId,
    string UpdateBookingDiscountSchemaReference,
    string UpdateBookingDetailsOperationId,
    string UpdateBookingDetailsSchemaReference,
    string UpdateBookingNotesOperationId,
    string UpdateBookingNotesSchemaReference,
    string ConfirmBookingOperationId,
    string CancelBookingOperationId,
    string CompleteBookingOperationId,
    string RecordPaymentOperationId,
    string RecordPaymentSchemaReference);
