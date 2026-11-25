namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Represents the subset of the bookings OpenAPI document that the contract consumer depends on.
/// </summary>
/// <param name="OpenApiVersion">Expected OpenAPI specification version of the document.</param>
/// <param name="Title">The API title declared in the info object.</param>
/// <param name="GetBookingPageOperationId">Expected operationId of the bounded booking page operation.</param>
/// <param name="GetBookingByIdOperationId">Expected operationId of the get booking by id operation.</param>
/// <param name="CreateBookingOperationId">Expected operationId of the create booking operation.</param>
/// <param name="CreateBookingSchemaReference">Expected $ref of the create booking request schema.</param>
/// <param name="DeleteBookingOperationId">Expected operationId of the delete booking operation.</param>
/// <param name="GetTourBookingPageOperationId">Expected operationId of the bounded tour booking history.</param>
/// <param name="GetCustomerBookingPageOperationId">Expected operationId of the bounded customer booking history.</param>
/// <param name="UpdateBookingDiscountOperationId">Expected operationId of the update booking discount operation.</param>
/// <param name="UpdateBookingDiscountSchemaReference">Expected $ref of the update booking discount request schema.</param>
/// <param name="UpdateBookingDetailsOperationId">Expected operationId of the update booking details operation.</param>
/// <param name="UpdateBookingDetailsSchemaReference">Expected $ref of the update booking details request schema.</param>
/// <param name="UpdateBookingNotesOperationId">Expected operationId of the update booking notes operation.</param>
/// <param name="UpdateBookingNotesSchemaReference">Expected $ref of the update booking notes request schema.</param>
/// <param name="ConfirmBookingOperationId">Expected operationId of the confirm booking operation.</param>
/// <param name="CancelBookingOperationId">Expected operationId of the cancel booking operation.</param>
/// <param name="CompleteBookingOperationId">Expected operationId of the complete booking operation.</param>
internal sealed record BookingsOpenApiContractDto(
    string OpenApiVersion,
    string Title,
    string GetBookingPageOperationId,
    string GetBookingByIdOperationId,
    string CreateBookingOperationId,
    string CreateBookingSchemaReference,
    string DeleteBookingOperationId,
    string GetTourBookingPageOperationId,
    string GetCustomerBookingPageOperationId,
    string UpdateBookingDiscountOperationId,
    string UpdateBookingDiscountSchemaReference,
    string UpdateBookingDetailsOperationId,
    string UpdateBookingDetailsSchemaReference,
    string UpdateBookingNotesOperationId,
    string UpdateBookingNotesSchemaReference,
    string ConfirmBookingOperationId,
    string CancelBookingOperationId,
    string CompleteBookingOperationId);
