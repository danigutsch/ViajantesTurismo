using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class TourConfirmBookingTests
{
    [Fact]
    public void ConfirmBooking_when_single_booking_exceeds_remaining_capacity_returns_conflict()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(1, 1)));
        var firstBooking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var secondBooking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        tour.ConfirmBooking(firstBooking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.ConfirmBooking(secondBooking.Id);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.Conflict);
        secondBooking.Status.ShouldBe(BookingStatus.Pending);
        tour.CurrentCustomerCount.ShouldBe(1);
    }

    [Fact]
    public void ConfirmBooking_when_double_booking_exceeds_remaining_capacity_returns_conflict()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(1, 2)));
        var firstBooking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var secondBooking = BookingTestHelpers.AddDoubleCustomerBooking(tour, null).Value;
        tour.ConfirmBooking(firstBooking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.ConfirmBooking(secondBooking.Id);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.Conflict);
        secondBooking.Status.ShouldBe(BookingStatus.Pending);
        tour.CurrentCustomerCount.ShouldBe(1);
    }

    [Fact]
    public void ConfirmBooking_when_booking_exactly_fills_capacity_succeeds()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(1, 2)));
        var booking = BookingTestHelpers.AddDoubleCustomerBooking(tour, null).Value;

        // Act
        var result = tour.ConfirmBooking(booking.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        tour.CurrentCustomerCount.ShouldBe(2);
    }

    [Fact]
    public void ConfirmBooking_when_booking_is_already_confirmed_remains_idempotent()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(1, 1)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var result = tour.ConfirmBooking(booking.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        tour.CurrentCustomerCount.ShouldBe(1);
    }
}
