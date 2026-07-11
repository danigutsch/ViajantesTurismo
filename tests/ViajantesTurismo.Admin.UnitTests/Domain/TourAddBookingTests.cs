using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourAddBookingTests
{
    [Fact]
    public void AddBooking_when_tour_is_fully_booked_returns_conflict_and_does_not_add_another_booking()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(MinCustomers: 1, MaxCustomers: 1)));
        var existingBookingResult = BookingTestHelpers.AddSingleCustomerBooking(
            tour,
            new SingleBookingOptions(CustomerId: Guid.CreateVersion7()));
        (existingBookingResult.IsSuccess).ShouldBeTrue("Failed to create existing booking for test setup.");
        (existingBookingResult.Value.Confirm().IsSuccess).ShouldBeTrue();
        var request = BookingDomainTestDataFactory.CreateValidSingleRequest();

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Tour is fully booked", StringComparison.Ordinal);
        (tour.Bookings).ShouldHaveSingleItem();
    }

    [Fact]
    public void AddBooking_when_principal_bike_type_is_invalid_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            (BikeType)999,
            RoomType.DoubleOccupancy,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid bike type", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_principal_bike_type_is_none_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.None,
            RoomType.DoubleOccupancy,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Bike type must be selected", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_companion_bike_type_is_invalid_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            principalCustomerId: Guid.CreateVersion7(),
            principalBikeType: BikeType.Regular,
            roomType: RoomType.DoubleOccupancy,
            discountType: DiscountType.None,
            companionCustomerId: Guid.CreateVersion7(),
            companionBikeType: (BikeType)999);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid bike type", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_companion_bike_type_is_missing_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            principalCustomerId: Guid.CreateVersion7(),
            principalBikeType: BikeType.Regular,
            roomType: RoomType.DoubleOccupancy,
            discountType: DiscountType.None,
            companionCustomerId: Guid.CreateVersion7(),
            companionBikeType: null);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Bike type must be selected", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_room_type_is_invalid_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            (RoomType)999,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid room type", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_single_room_has_companion_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = BookingDomainTestDataFactory.CreateValidDoubleRequest(roomType: RoomType.SingleOccupancy);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot have a companion", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_principal_and_companion_are_the_same_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var customerId = Guid.CreateVersion7();
        var request = BookingDomainTestDataFactory.CreateValidDoubleRequest(
            principalCustomerId: customerId,
            companionCustomerId: customerId);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be the same person", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_discount_is_invalid_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = TourBookingRequest.CreateSingle(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.Percentage(150m, "Seasonal promo"));

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot exceed", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_absolute_discount_exceeds_subtotal_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = TourBookingRequest.CreateSingle(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            new BookingDiscountDefinition(DiscountType.Absolute, 50_000m, "Manual approval"));

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot exceed subtotal", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_notes_exceed_maximum_length_returns_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var tooLongNotes = new string('n', 2001);
        var request = TourBookingRequest.CreateSingle(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.None,
            tooLongNotes);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Notes cannot exceed", StringComparison.Ordinal);
        (tour.Bookings).ShouldBeEmpty();
    }

    [Fact]
    public void AddBooking_when_request_is_valid_adds_booking_to_tour()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = BookingDomainTestDataFactory.CreateValidDoubleRequest();

        // Act
        var result = tour.AddBooking(request);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        var booking = (tour.Bookings).ShouldHaveSingleItem();
        (booking.Id).ShouldBe(result.Value.Id);
        (booking.CompanionCustomer).ShouldNotBeNull();
        (booking.PrincipalCustomer.CustomerId).ShouldBe(request.Travelers.PrincipalCustomerId);
        (booking.CompanionCustomer.CustomerId).ShouldBe(request.Travelers.CompanionCustomerId);
        (booking.RoomType).ShouldBe(request.RoomType);
    }

}
