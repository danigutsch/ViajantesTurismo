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

        contract.OpenApiVersion.ShouldBe("3.2.0");
        contract.Title.ShouldBe("ViajantesTurismo.Admin.Api | bookings");
        contract.ListBookingsOperationId.ShouldBe("GetBookings");
        contract.GetBookingPageOperationId.ShouldBe("GetBookingPage");
        contract.GetBookingByIdOperationId.ShouldBe("GetBookingById");
        contract.CreateBookingOperationId.ShouldBe("CreateBooking");
        contract.CreateBookingSchemaReference.ShouldBe("#/components/schemas/CreateBookingDto");
        contract.DeleteBookingOperationId.ShouldBe("DeleteBooking");
        contract.GetBookingsByTourIdOperationId.ShouldBe("GetBookingsByTourId");
        contract.GetBookingsByCustomerIdOperationId.ShouldBe("GetBookingsByCustomerId");
        contract.GetTourBookingPageOperationId.ShouldBe("GetTourBookingPage");
        contract.GetCustomerBookingPageOperationId.ShouldBe("GetCustomerBookingPage");
        contract.UpdateBookingDiscountOperationId.ShouldBe("UpdateBookingDiscount");
        contract.UpdateBookingDiscountSchemaReference.ShouldBe("#/components/schemas/UpdateBookingDiscountDto");
        contract.UpdateBookingDetailsOperationId.ShouldBe("UpdateBookingDetails");
        contract.UpdateBookingDetailsSchemaReference.ShouldBe("#/components/schemas/UpdateBookingDetailsDto");
        contract.UpdateBookingNotesOperationId.ShouldBe("UpdateBookingNotes");
        contract.UpdateBookingNotesSchemaReference.ShouldBe("#/components/schemas/UpdateBookingNotesDto");
        contract.ConfirmBookingOperationId.ShouldBe("ConfirmBooking");
        contract.CancelBookingOperationId.ShouldBe("CancelBooking");
        contract.CompleteBookingOperationId.ShouldBe("CompleteBooking");
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
