using SharedKernel.Testing.Assertions;
using SharedKernel.Testing.Contracts;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Shared assertions for the bookings OpenAPI contract slice.
/// </summary>
internal static class BookingsOpenApiAssertions
{
    /// <summary>
    /// Verifies the canonical bookings contract slice values that consumers rely on.
    /// </summary>
    public static void MatchesCanonicalConsumerSlice(BookingsOpenApiContractDto contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        contract.OpenApiVersion.ShouldBe("3.1.1");
        contract.Title.ShouldBe("ViajantesTurismo.Admin.ApiService | bookings");
        contract.ListBookingsOperationId.ShouldBe("GetBookings");
        contract.GetBookingByIdOperationId.ShouldBe("GetBookingById");
        contract.CreateBookingOperationId.ShouldBe("CreateBooking");
        contract.CreateBookingSchemaReference.ShouldBe("#/components/schemas/CreateBookingDto");
        contract.DeleteBookingOperationId.ShouldBe("DeleteBooking");
        contract.GetBookingsByTourIdOperationId.ShouldBe("GetBookingsByTourId");
        contract.GetBookingsByCustomerIdOperationId.ShouldBe("GetBookingsByCustomerId");
        contract.UpdateBookingDiscountOperationId.ShouldBe("UpdateBookingDiscount");
        contract.UpdateBookingDiscountSchemaReference.ShouldBe("#/components/schemas/UpdateBookingDiscountDto");
        contract.UpdateBookingDetailsOperationId.ShouldBe("UpdateBookingDetails");
        contract.UpdateBookingDetailsSchemaReference.ShouldBe("#/components/schemas/UpdateBookingDetailsDto");
        contract.UpdateBookingNotesOperationId.ShouldBe("UpdateBookingNotes");
        contract.UpdateBookingNotesSchemaReference.ShouldBe("#/components/schemas/UpdateBookingNotesDto");
        contract.ConfirmBookingOperationId.ShouldBe("ConfirmBooking");
        contract.CancelBookingOperationId.ShouldBe("CancelBooking");
        contract.CompleteBookingOperationId.ShouldBe("CompleteBooking");
        contract.RecordPaymentOperationId.ShouldBe("RecordPayment");
        contract.RecordPaymentSchemaReference.ShouldBe("#/components/schemas/CreatePaymentDto");
    }

    /// <summary>
    /// Verifies that the generated bookings artifact stays compatible with the canonical artifact.
    /// </summary>
    public static void MatchesGeneratedArtifact(
        BookingsOpenApiContractDto canonicalContract,
        BookingsOpenApiContractDto generatedContract)
    {
        ArgumentNullException.ThrowIfNull(canonicalContract);
        ArgumentNullException.ThrowIfNull(generatedContract);

        ContractArtifactAssertions.MatchesGeneratedArtifact(canonicalContract, generatedContract);
    }
}
