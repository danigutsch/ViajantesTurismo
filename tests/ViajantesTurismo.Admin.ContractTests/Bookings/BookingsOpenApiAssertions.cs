namespace ViajantesTurismo.Admin.ContractTests.Bookings;

internal static class BookingsOpenApiAssertions
{
    public static void MatchesCanonicalConsumerSlice(BookingsOpenApiContractDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        Xunit.Assert.Equal("3.1.1", contract.OpenApiVersion);
        Xunit.Assert.Equal("ViajantesTurismo.Admin.ApiService | bookings", contract.Title);
        Xunit.Assert.Equal("GetBookings", contract.ListBookingsOperationId);
        Xunit.Assert.Equal("GetBookingById", contract.GetBookingByIdOperationId);
        Xunit.Assert.Equal("#/components/schemas/CreateBookingDto", contract.CreateBookingSchemaReference);
        Xunit.Assert.Equal("DeleteBooking", contract.DeleteBookingOperationId);
        Xunit.Assert.Equal("GetBookingsByTourId", contract.GetBookingsByTourIdOperationId);
        Xunit.Assert.Equal("GetBookingsByCustomerId", contract.GetBookingsByCustomerIdOperationId);
        Xunit.Assert.Equal("#/components/schemas/UpdateBookingDiscountDto", contract.UpdateBookingDiscountSchemaReference);
        Xunit.Assert.Equal("#/components/schemas/UpdateBookingDetailsDto", contract.UpdateBookingDetailsSchemaReference);
        Xunit.Assert.Equal("#/components/schemas/UpdateBookingNotesDto", contract.UpdateBookingNotesSchemaReference);
        Xunit.Assert.Equal("ConfirmBooking", contract.ConfirmBookingOperationId);
        Xunit.Assert.Equal("CancelBooking", contract.CancelBookingOperationId);
        Xunit.Assert.Equal("CompleteBooking", contract.CompleteBookingOperationId);
        Xunit.Assert.Equal("RecordPayment", contract.RecordPaymentOperationId);
        Xunit.Assert.Equal("#/components/schemas/CreatePaymentDto", contract.RecordPaymentSchemaReference);
    }

    public static void MatchesGeneratedArtifact(
        BookingsOpenApiContractDto canonicalContract,
        BookingsOpenApiContractDto generatedContract)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(generatedContract);

        Xunit.Assert.Equal(canonicalContract, generatedContract);
    }
}
