using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class TourCustomerDocumentEligibilityTests
{
    [Fact]
    public void A_confirmed_booking_can_receive_customer_documents()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.CanReceiveCustomerDocuments(booking.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_completed_booking_can_receive_customer_documents()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.CanReceiveCustomerDocuments(booking.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_pending_booking_cannot_receive_customer_documents()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;

        // Act
        var result = tour.CanReceiveCustomerDocuments(booking.Id);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            "A customer-facing document draft requires a confirmed or completed booking.");
    }

    [Fact]
    public void A_cancelled_booking_cannot_receive_customer_documents()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.CanReceiveCustomerDocuments(booking.Id);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public void A_booking_outside_the_tour_cannot_receive_customer_documents()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.CanReceiveCustomerDocuments(Guid.CreateVersion7());

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }
}
