using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
public sealed class BookingCanModifyBookedServicesTests
{
    [Fact]
    public void Pending_booking_can_modify_booked_services()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();

        // Act
        var canModify = booking.CanModifyBookedServices;

        // Assert
        canModify.ShouldBeTrue();
    }

    [Fact]
    public void Confirmed_booking_can_modify_booked_services()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canModify = booking.CanModifyBookedServices;

        // Assert
        canModify.ShouldBeTrue();
    }

    [Fact]
    public void Cancelled_booking_cannot_modify_booked_services()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canModify = booking.CanModifyBookedServices;

        // Assert
        canModify.ShouldBeFalse();
    }

    [Fact]
    public void Completed_booking_cannot_modify_booked_services()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canModify = booking.CanModifyBookedServices;

        // Assert
        canModify.ShouldBeFalse();
    }

    [Fact]
    public void Cancelled_booking_rejects_a_discount_change_that_the_fact_forbids()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();
        var canModify = booking.CanModifyBookedServices;

        // Act
        var result = tour.UpdateBookingDiscount(booking.Id, DiscountType.Percentage, 10m, "Loyalty");

        // Assert
        canModify.ShouldBeFalse();
        result.IsSuccess.ShouldBeFalse();
    }
}
